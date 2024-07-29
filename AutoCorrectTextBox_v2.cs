using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using TechCommand.Core.Helpers;
using TechCommand.Core.Services;

namespace TechCommand.Controls;

public class AutoCorrectTextBox : TextBox
{
    private readonly Thesaurus _thesaurus;
    private readonly DispatcherTimer _typingTimer;
    private const int TypingDelay = 500; // milliseconds
    private readonly HashSet<string> _customDictionary;

    public bool IsAutoCorrectEnabled { get; set; } = true;

    public AutoCorrectTextBox(Thesaurus thesaurus)
    {
        _thesaurus = thesaurus ?? throw new ArgumentNullException(nameof(thesaurus));
        _customDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _typingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(TypingDelay)
        };

        _typingTimer.Tick += OnTypingTimerTick;

        this.TextChanged += OnAutoCorrectTextBoxTextChanged;
    }

    private void OnAutoCorrectTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsAutoCorrectEnabled) return;

        _typingTimer.Stop();
        _typingTimer.Start();
    }

    private void OnTypingTimerTick(object sender, object e)
    {
        _typingTimer.Stop();
        ProcessText();
    }

    private void ProcessText()
    {
        try
        {
            var text = this.Text;
            var words = GetWords(text);
            var correctedWords = new List<string>();
            var misspelledRanges = new List<(int start, int length)>();

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (IsValidWord(word))
                {
                    correctedWords.Add(word);
                }
                else
                {
                    var suggestions = GetContextAwareSuggestions(words, i);
                    var correctedWord = suggestions.FirstOrDefault() ?? word;
                    correctedWords.Add(correctedWord);

                    if (correctedWord == word)
                    {
                        var startIndex = GetCorrectedTextLength(correctedWords) - word.Length;
                        misspelledRanges.Add((startIndex, word.Length));
                    }
                }
            }

            var correctedText = string.Join("", correctedWords);
            UpdateTextWithUnderlines(correctedText, misspelledRanges);
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error in ProcessText {ex.Message}", LogLevel.Error);
        }
    }

    private List<string> GetWords(string text)
    {
        return Regex.Split(text, @"\b").Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
    }

    private bool IsValidWord(string word)
    {
        return _thesaurus.IsValidWord(word) || _customDictionary.Contains(word);
    }

    private List<string> GetContextAwareSuggestions(List<string> words, int currentIndex)
    {
        var currentWord = words[currentIndex];
        var context = GetContext(words, currentIndex);

        var suggestions = _thesaurus.GetSynonyms(currentWord);

        var scoredSuggestions = suggestions.Select(s => new
        {
            Word = s,
            Score = context.Count(c => _thesaurus.GetSynonyms(c).Contains(s))
        })
        .OrderByDescending(s => s.Score)
        .ThenBy(s => LevenshteinDistance(currentWord, s.Word));

        return scoredSuggestions.Select(s => s.Word).Take(5).ToList();
    }

    private List<string> GetContext(List<string> words, int currentIndex)
    {
        var context = new List<string>();

        for (var i = Math.Max(0, currentIndex - 2); i < Math.Min(words.Count, currentIndex + 3); i++)
        {
            if (i != currentIndex)
            {
                context.Add(words[i]);
            }
        }

        return context;
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
                {
                    paragraph.Inlines.Add(new Run { Text = correctedText.Substring(lastIndex, start - lastIndex) });
                }

                var misspelledRun = new Run { Text = correctedText.Substring(start, length) };
                misspelledRun.TextDecorations = TextDecorations.Underline;
                paragraph.Inlines.Add(misspelledRun);

                lastIndex = start + length;
            }

            if (lastIndex < correctedText.Length)
            {
                paragraph.Inlines.Add(new Run { Text = correctedText.Substring(lastIndex) });
            }

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

    private int GetCorrectedTextLength(List<string> correctedWords)
    {
        return string.Join("", correctedWords).Length;
    }

    public void AddToCustomDictionary(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException("Word cannot be null or whitespace.", nameof(word));

        try
        {
            _customDictionary.Add(word.ToLower());
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
        if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException("Word cannot be null or whitespace.", nameof(word));

        try
        {
            _customDictionary.Remove(word.ToLower());
            Logger.Instance.Log($"Removed '{word}' from custom dictionary");
            ProcessText();
        }
        catch (Exception ex)
        {
            Logger.Instance.Log($"Error removing '{word}' from custom dictionary", LogLevel.Error);
        }
    }
}
