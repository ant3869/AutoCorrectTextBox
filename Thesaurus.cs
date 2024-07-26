using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechCommand.Core.Helpers;

namespace TechCommand.Core.Services;

/* EXAMPLE
    var thesaurus = new Thesaurus();

    // Load data
    thesaurus.LoadFromNHunspellThesaurus("path/to/the_en_us.dat");
    thesaurus.LoadDictionary("path/to/en_us.dic");

    // Get synonyms for a word
    var synonyms = thesaurus.GetSynonyms("example", PartOfSpeech.Noun);
    foreach (var synonym in synonyms)
    {
        Console.WriteLine($"{synonym.Text} ({synonym.PartOfSpeech}) - Used {synonym.UsageFrequency} times");
    }

    // Add a phrase
    thesaurus.AddPhrase("in a nutshell", new List<string> { "briefly", "concisely", "in summary" }, PartOfSpeech.Adverb);

    // Get synonyms sorted by usage
    var sortedSynonyms = thesaurus.GetSynonymsSortedByUsage("big");
    foreach (var synonym in sortedSynonyms)
    {
        Console.WriteLine($"{synonym.Text} - Used {synonym.UsageFrequency} times");
    }

    // Export the thesaurus
    thesaurus.ExportToJson("thesaurus_export.json");
    thesaurus.ExportToXml("thesaurus_export.xml");



    public sealed partial class MainPage : Page
    {
        private Thesaurus thesaurus;

        public MainPage()
        {
            this.InitializeComponent();
            thesaurus = new Thesaurus();
            // Load your thesaurus data here
        }

        private void LookupButton_Click(object sender, RoutedEventArgs e)
        {
            var word = WordTextBox.Text;
            var synonyms = thesaurus.GetSynonymsSortedByUsage(word);
            SynonymsListView.ItemsSource = synonyms;
        }
    }
*/


public enum PartOfSpeech { Noun, Verb, Adjective, Adverb, Other }

public class Word
{
    public string Text { get; set; }
    public PartOfSpeech PartOfSpeech { get; set; }
    public List<string> Forms { get; set; } = new List<string>();
    public int UsageFrequency { get; set; } = 0;
}

public class Thesaurus
{
    private Dictionary<string, HashSet<Word>> synonymDictionary;
    private HashSet<string> validWords;
    private Dictionary<string, List<Word>> cache;
    private const int CacheSize = 1000;

    public Thesaurus()
    {
        synonymDictionary = new Dictionary<string, HashSet<Word>>(StringComparer.OrdinalIgnoreCase);
        validWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        cache = new Dictionary<string, List<Word>>(StringComparer.OrdinalIgnoreCase);
    }

    public void LoadFromNHunspellThesaurus(string filePath)
    {
        try
        {
            var currentWord = string.Empty;
            var currentPos = PartOfSpeech.Other;

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("(") || string.IsNullOrWhiteSpace(line)) continue;

                if (!line.StartsWith(" "))
                {
                    var parts = line.Split('|');
                    currentWord = parts[0].Trim().ToLower();
                    currentPos = ParsePartOfSpeech(parts.Length > 1 ? parts[1] : string.Empty);

                    if (!synonymDictionary.ContainsKey(currentWord))
                        synonymDictionary[currentWord] = new HashSet<Word>();
                }
                else
                {
                    var synonyms = line.Trim().Split(',').Select(s => s.Trim().ToLower());

                    foreach (var synonym in synonyms)
                    {
                        AddSynonymWithPos(currentWord, synonym, currentPos);
                    }
                }
            }
            Logger.Instance.Log("NHunspell thesaurus loaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Exception while loading NHunspell thesaurus: {ex.Message}", LogLevel.Error);
        }
    }

    public void LoadDictionary(string filePath)
    {
        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('/');
                var word = parts[0].Trim().ToLower();
                validWords.Add(word);

                if (parts.Length > 1)
                {
                    var forms = GenerateWordForms(word, parts[1]);
                    validWords.UnionWith(forms);
                }
            }
            Logger.Instance.Log("Dictionary loaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Exception while loading dictionary: {ex.Message}", LogLevel.Error);
        }
    }

    private IEnumerable<string> GenerateWordForms(string word, string flags)
    {
        var forms = new List<string> { word };

        if (flags.Contains("S")) forms.Add(Pluralize(word));

        if (flags.Contains("V")) 
        {
            forms.Add(PastTense(word));
            forms.Add(PresentParticiple(word));
        }

        if (flags.Contains("A")) forms.Add(Comparative(word));

        return forms;
    }

    private string Pluralize(string word)
    {
        if (word.EndsWith("y")) return word.Substring(0, word.Length - 1) + "ies";
        if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("z") || word.EndsWith("ch") || word.EndsWith("sh"))
            return word + "es";
        return word + "s";
    }

    private string PastTense(string word)
    {
        if (word.EndsWith("e")) return word + "d";
        if (word.EndsWith("y")) return word.Substring(0, word.Length - 1) + "ied";
        return word + "ed";
    }

    private string PresentParticiple(string word)
    {
        if (word.EndsWith("e")) return word.Substring(0, word.Length - 1) + "ing";
        return word + "ing";
    }

    private string Comparative(string word)
    {
        if (word.EndsWith("y")) return word.Substring(0, word.Length - 1) + "ier";
        if (word.Length > 1) return word + "er";
        return word;
    }

    private PartOfSpeech ParsePartOfSpeech(string pos)
    {
        switch (pos.ToLower())
        {
            case "n": return PartOfSpeech.Noun;
            case "v": return PartOfSpeech.Verb;
            case "adj": return PartOfSpeech.Adjective;
            case "adv": return PartOfSpeech.Adverb;
            default: return PartOfSpeech.Other;
        }
    }

    public IEnumerable<Word> GetSynonyms(string word, PartOfSpeech? pos = null)
    {
        word = word.ToLower();

        if (cache.TryGetValue(word, out var cachedSynonyms))
        {
            UpdateUsageFrequency(cachedSynonyms);
            return pos.HasValue ? cachedSynonyms.Where(s => s.PartOfSpeech == pos.Value) : cachedSynonyms;
        }

        if (synonymDictionary.TryGetValue(word, out var synonyms))
        {
            var result = pos.HasValue 
                ? synonyms.Where(s => s.PartOfSpeech == pos.Value && validWords.Contains(s.Text)).ToList()
                : synonyms.Where(s => validWords.Contains(s.Text)).ToList();

            UpdateCache(word, result);
            UpdateUsageFrequency(result);
            return result;
        }

        Logger.Instance.Log($"No synonyms found for '{word}'", LogLevel.Warning);

        return Enumerable.Empty<Word>();
    }

    private void UpdateCache(string word, List<Word> synonyms)
    {
        if (cache.Count >= CacheSize)
        {
            var leastUsed = cache.OrderBy(kvp => kvp.Value.Sum(w => w.UsageFrequency)).First().Key;
            cache.Remove(leastUsed);
        }

        cache[word] = synonyms;
    }

    private void UpdateUsageFrequency(IEnumerable<Word> words)
    {
        foreach (var word in words)
        {
            word.UsageFrequency++;
        }
    }

    public bool IsValidWord(string word)
    {
        return validWords.Contains(word.ToLower());
    }

    public void AddSynonymWithPos(string word, string synonym, PartOfSpeech pos)
    {
        word = word.ToLower();
        synonym = synonym.ToLower();

        if (!synonymDictionary.ContainsKey(word))
            synonymDictionary[word] = new HashSet<Word>();

        synonymDictionary[word].Add(new Word { Text = synonym, PartOfSpeech = pos });

        if (!synonymDictionary.ContainsKey(synonym))
            synonymDictionary[synonym] = new HashSet<Word>();

        synonymDictionary[synonym].Add(new Word { Text = word, PartOfSpeech = pos });

        cache.Remove(word);
        cache.Remove(synonym);

        Logger.Instance.Log($"Added synonym: '{word}' - '{synonym}' ({pos})");
    }

    public void RemoveSynonym(string word, string synonym)
    {
        word = word.ToLower();
        synonym = synonym.ToLower();

        if (synonymDictionary.ContainsKey(word))
            synonymDictionary[word].RemoveWhere(w => w.Text == synonym);

        if (synonymDictionary.ContainsKey(synonym))
            synonymDictionary[synonym].RemoveWhere(w => w.Text == word);

        cache.Remove(word);
        cache.Remove(synonym);

        Logger.Instance.Log($"Removed synonym: '{word}' - '{synonym}'");
    }

    public void AddPhrase(string phrase, List<string> synonyms, PartOfSpeech pos)
    {
        phrase = phrase.ToLower();

        if (!synonymDictionary.ContainsKey(phrase))
            synonymDictionary[phrase] = new HashSet<Word>();

        foreach (var synonym in synonyms)
            AddSynonymWithPos(phrase, synonym, pos);

        Logger.Instance.Log($"Added phrase: '{phrase}' with {synonyms.Count} synonyms");
    }

    public void ExportToJson(string filePath)
    {
        try
        {
            var export = synonymDictionary.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(w => new { w.Text, w.PartOfSpeech, w.UsageFrequency }).ToList()
            );

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            Logger.Instance.Log($"Thesaurus exported to JSON: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error exporting thesaurus to JSON: {ex.Message}", LogLevel.Error);
        }
    }

    public void ExportToXml(string filePath)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(Dictionary<string, HashSet<Word>>));

            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, synonymDictionary);
            }

            Logger.Instance.Log($"Thesaurus exported to XML: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error exporting thesaurus to XML: {ex.Message}", LogLevel.Error);
        }
    }

    public List<Word> GetSynonymsSortedByUsage(string word, PartOfSpeech? pos = null)
    {
        return GetSynonyms(word, pos).OrderByDescending(w => w.UsageFrequency).ToList();
    }
}
