using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.System;
using Microsoft.Extensions.Logging;
using TechCommand.Core.Helpers;
using TechCommand.Core.Services;

namespace TechCommand.Controls;

public class AutoCorrectTextBox : TextBox
{
    private Thesaurus thesaurus;
    private readonly DispatcherTimer typingTimer;
    private const int TypingDelay = 500; // milliseconds
    private HashSet<string> customDictionary;

    public bool IsAutoCorrectEnabled { get; set; } = true;

    public AutoCorrectTextBox(Thesaurus thesaurus)
    {
        this.thesaurus = thesaurus;
        customDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        typingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(TypingDelay)
        };

        typingTimer.Tick += TypingTimer_Tick;

        this.TextChanged += AutoCorrectTextBox_TextChanged;
    }

    private void AutoCorrectTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (!IsAutoCorrectEnabled) return;

            typingTimer.Stop();
            typingTimer.Start();
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error in AutoCorrectTextBox_TextChanged {ex.Message}", LogLevel.Error);
        }
    }

    private void TypingTimer_Tick(object sender, object e)
    {
        try
        {
            typingTimer.Stop();
            ProcessText();
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error in TypingTimer_Tick {ex.Message}", LogLevel.Error);
        }
    }

    private void ProcessText()
    {
        var text = this.Text;
        var words = Regex.Split(text, @"\b").Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        var correctedWords = new List<string>();
        var misspelledRanges = new List<(int start, int length)>();

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];

            if (IsValidWord(word))
                correctedWords.Add(word);
            else
            {
                var suggestions = GetContextAwareSuggestions(words, i);
                var correctedWord = suggestions.FirstOrDefault() ?? word;
                correctedWords.Add(correctedWord);

                if (correctedWord == word)
                {
                    var startIndex = string.Join("", correctedWords).Length - word.Length;
                    misspelledRanges.Add((startIndex, word.Length));
                }
            }
        }

        var correctedText = string.Join("", correctedWords);
        UpdateTextWithUnderlines(correctedText, misspelledRanges);
    }

    private bool IsValidWord(string word)
    {
        return thesaurus.IsValidWord(word) || customDictionary.Contains(word);
    }

    private List<string> GetContextAwareSuggestions(List<string> words, int currentIndex)
    {
        var currentWord = words[currentIndex];
        var context = new List<string>();

        // Get up to two words before and after the current word for context
        for (var i = Math.Max(0, currentIndex - 2); i < Math.Min(words.Count, currentIndex + 3); i++)
        {
            if (i != currentIndex)
                context.Add(words[i]);
        }

        var suggestions = thesaurus.GetSynonyms(currentWord);

        // Score suggestions based on context
        var scoredSuggestions = suggestions.Select(s => new
        {
            Word = s,
            Score = context.Count(c => thesaurus.GetSynonyms(c).Contains(s))
        }).OrderByDescending(s => s.Score).ThenBy(s => LevenshteinDistance(currentWord, s.Word));

        return scoredSuggestions.Select(s => s.Word).Take(5).ToList();
    }

    private int LevenshteinDistance(string s, string t)
    {
        int[,] d = new int[s.Length + 1, t.Length + 1];

        for (int i = 0; i <= s.Length; i++)
            d[i, 0] = i;

        for (int j = 0; j <= t.Length; j++)
            d[0, j] = j;

        for (int j = 1; j <= t.Length; j++)
            for (int i = 1; i <= s.Length; i++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + ((s[i - 1] == t[j - 1]) ? 0 : 1));

        return d[s.Length, t.Length];
    }

    private void UpdateTextWithUnderlines(string correctedText, List<(int start, int length)> misspelledRanges)
    {
        try
        {
            var selectionStart = this.SelectionStart;
            var textBlock = new RichTextBlock();
            var paragraph = new Paragraph();

            var lastIndex = 0;

            foreach (var (start, length) in misspelledRanges)
            {
                if (start > lastIndex)
                    paragraph.Inlines.Add(new Run { Text = correctedText.Substring(lastIndex, start - lastIndex) });

                var misspelledRun = new Run { Text = correctedText.Substring(start, length) };
                misspelledRun.TextDecorations = TextDecorations.Underline;
                paragraph.Inlines.Add(misspelledRun);

                lastIndex = start + length;
            }

            if (lastIndex < correctedText.Length)
                paragraph.Inlines.Add(new Run { Text = correctedText.Substring(lastIndex) });

            textBlock.Blocks.Add(paragraph);

            this.Document = textBlock.GetXaml();
            this.SelectionStart = selectionStart;
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error in UpdateTextWithUnderline {ex.Message}", LogLevel.Error);
            this.Text = correctedText;
        }
    }

    public void AddToCustomDictionary(string word)
    {
        try
        {
            customDictionary.Add(word.ToLower());
            Logger.Instance.Log($"Added '{word}' to custom dictionary");
            ProcessText();
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error adding '{word}' to custom dictionary", LogLevel.Error);
        }
    }

    public void RemoveFromCustomDictionary(string word)
    {
        try
        {
            customDictionary.Remove(word.ToLower());
            Logger.Instance.Log($"Removed '{word}' from custom dictionary");
            ProcessText();
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error removing '{word}' from custom dictionary", LogLevel.Error);      
        }
    }
}
