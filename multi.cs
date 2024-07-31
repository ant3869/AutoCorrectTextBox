using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoCorrectLibrary;

namespace AutoCorrectApp
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void AutoCorrectTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var cursorPosition = textBox.SelectionStart;
            textBox.TextChanged -= AutoCorrectTextBox_TextChanged;  // Unsubscribe to avoid recursive call
            textBox.Text = TextHelper.ContextualAutoCorrect(textBox.Text);
            textBox.SelectionStart = cursorPosition;  // Restore cursor position
            textBox.TextChanged += AutoCorrectTextBox_TextChanged;  // Subscribe again
        }
    }
}




<Application
    x:Class="AutoCorrectApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <!-- Application resource dictionary -->
    </Application.Resources>
</Application>




using Microsoft.UI.Xaml;

namespace AutoCorrectApp
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private Window m_window;
    }
}



private static Dictionary<string, string> _corrections = new Dictionary<string, string>
{
    { "teh", "the" },
    { "recieve", "receive" },
    // Add more pairs here
};
