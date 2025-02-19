using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace VoidTerminal
{
    public partial class OutputWindow : Window
    {
        private TextBox? outputTextBox;

        public OutputWindow()
        {
            InitializeComponent();
            outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        }

        public void SetContent(string title, string content)
        {
            Title = title;
            outputTextBox!.Text = content;
        }
    }
} 