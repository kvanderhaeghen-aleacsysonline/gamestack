using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gamestack.App.Views;

/// <summary>A simple modal Yes/No dialog returning a bool result.</summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog() => InitializeComponent();

    public ConfirmDialog(string title, string message) : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void OnYes(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNo(object? sender, RoutedEventArgs e) => Close(false);
}
