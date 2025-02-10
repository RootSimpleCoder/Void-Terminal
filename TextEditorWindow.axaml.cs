using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VoidTerminal
{
    public partial class TextEditorWindow : Window
    {
        private string filePath = string.Empty;
        private TextBox? editorTextBox;
        private bool hasUnsavedChanges = false;

        public TextEditorWindow()
        {
            InitializeComponent();
            editorTextBox = this.FindControl<TextBox>("EditorTextBox");
            
            editorTextBox!.TextChanged += (s, e) => 
            {
                hasUnsavedChanges = true;
                UpdateTitle();
            };
        }

        public void LoadFile(string path)
        {
            filePath = path;
            if (File.Exists(path))
            {
                editorTextBox!.Text = File.ReadAllText(path);
            }
            hasUnsavedChanges = false;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Title = $"Editing: {Path.GetFileName(filePath)}{(hasUnsavedChanges ? " *" : "")}";
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveFile()
        {
            try
            {
                File.WriteAllText(filePath, editorTextBox!.Text);
                hasUnsavedChanges = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                ShowError($"Error saving file: {ex.Message}");
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                e.Cancel = true;
                ShowUnsavedChangesDialog();
            }
            base.OnClosing(e);
        }

        private async void ShowUnsavedChangesDialog()
        {
            var yesButton = new Button { Content = "Yes" };
            var noButton = new Button { Content = "No" };
            var cancelButton = new Button { Content = "Cancel" };

            var dialog = new Window
            {
                Title = "Unsaved Changes",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.BorderOnly,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "You have unsaved changes. Do you want to save before closing?",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children =
                            {
                                yesButton,
                                noButton,
                                cancelButton
                            }
                        }
                    }
                }
            };

            var tcs = new TaskCompletionSource<string>();

            yesButton.Click += (s, e) => 
            {
                tcs.SetResult("Yes");
                dialog.Close();
            };

            noButton.Click += (s, e) => 
            {
                tcs.SetResult("No");
                dialog.Close();
            };

            cancelButton.Click += (s, e) => 
            {
                tcs.SetResult("Cancel");
                dialog.Close();
            };

            await dialog.ShowDialog(this);
            var result = await tcs.Task;
            
            switch (result)
            {
                case "Yes":
                    SaveFile();
                    hasUnsavedChanges = false;
                    Close();
                    break;
                case "No":
                    hasUnsavedChanges = false;
                    Close();
                    break;
            }
        }

        private async void ShowError(string message)
        {
            var dialog = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            await dialog.ShowDialog(this);
        }
    }
} 