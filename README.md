# AutoCorrectTextBox
A custom winui3 textbox control with real-time auto-correct feature.

# Features
### Context Awareness
The GetContextAwareSuggestions method now considers surrounding words when making suggestions. It scores suggestions based on their relevance to the context and also uses Levenshtein distance as a tie-breaker.
### Custom Dictionary
A customDictionary HashSet is added to store user-defined words. The AddToCustomDictionary and RemoveFromCustomDictionary methods allow manipulation of this dictionary.
Visual Feedback: The UpdateTextWithUnderlines method now underlines potentially misspelled words using rich text formatting.
### Error Handling and Logging
Robust try-catch blocks have been added throughout the class, with errors being logged using the injected ILogger.
Performance Optimization: The text processing now only splits on word boundaries and ignores whitespace, which should improve performance for large texts.

# How to use in your WinUI 3 application
Install the Microsoft.Extensions.Logging NuGet package.
Initialize the control with both the Thesaurus and an ILogger.

```
var logger = loggerFactory.CreateLogger<AutoCorrectTextBox>();
WorkNotesTextBox = new AutoCorrectTextBox(thesaurus, logger);
```

# Impliment custom dictionary 
Add a context menu item.

```
WorkNotesTextBox.ContextFlyout = new MenuFlyout();
var addToDictionaryItem = new MenuFlyoutItem { Text = "Add to Dictionary" };
addToDictionaryItem.Click += (s, e) => 
{
    var selectedWord = WorkNotesTextBox.SelectedText;
    if (!string.IsNullOrWhiteSpace(selectedWord))
    {
        WorkNotesTextBox.AddToCustomDictionary(selectedWord);
    }
};
WorkNotesTextBox.ContextFlyout.Items.Add(addToDictionaryItem);
```

<local:AutoCorrectTextBox x:Name="WorkNotesTextBox" Height="100" Width="300" />

WorkNotesTextBox = new AutoCorrectTextBox(thesaurus);

<ToggleSwitch x:Name="AutoCorrectToggle" Header="Auto-Correct" 
              IsOn="{x:Bind WorkNotesTextBox.IsAutoCorrectEnabled, Mode=TwoWay}"/>
