using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gamestack.App.Views;

/// <summary>A simple modal text-input dialog returning the entered string, or null if cancelled.</summary>
public partial class PromptDialog : Window
{
    public PromptDialog() => InitializeComponent();

    public PromptDialog(string title, string message, string defaultValue = "") : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
        Input.Text = defaultValue;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(Input.Text);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}
