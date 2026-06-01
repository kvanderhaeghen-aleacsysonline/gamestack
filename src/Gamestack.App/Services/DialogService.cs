using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Gamestack.App.Views;

namespace Gamestack.App.Services;

/// <summary>Shows simple modal dialogs owned by the main window.</summary>
public sealed class DialogService
{
    /// <summary>Show a Yes/No confirmation. Returns true if the user chose Yes.</summary>
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var dialog = new ConfirmDialog(title, message);
        if (owner is null)
        {
            dialog.Show();
            return false;
        }

        // If the app is minimized to the tray, restore the window so it can own the modal dialog.
        if (!owner.IsVisible)
        {
            owner.Show();
            owner.WindowState = WindowState.Normal;
            owner.Activate();
        }

        return await dialog.ShowDialog<bool>(owner);
    }

    /// <summary>Show a single-line text prompt. Returns the entered text, or null if cancelled.</summary>
    public async Task<string?> PromptAsync(string title, string message, string defaultValue = "")
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var dialog = new PromptDialog(title, message, defaultValue);
        if (owner is null)
        {
            dialog.Show();
            return null;
        }

        if (!owner.IsVisible)
        {
            owner.Show();
            owner.WindowState = WindowState.Normal;
            owner.Activate();
        }

        return await dialog.ShowDialog<string?>(owner);
    }
}
