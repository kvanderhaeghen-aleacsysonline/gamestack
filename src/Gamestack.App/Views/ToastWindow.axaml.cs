using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Gamestack.App.Views;

/// <summary>A small, auto-dismissing toast shown in the bottom-right corner (desktop notification).</summary>
public partial class ToastWindow : Window
{
    private Action? _onOpen;

    public ToastWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    /// <summary>Create and show a toast with the given title/body and an "Open" action.</summary>
    public static void Show(string title, string body, Action onOpen)
    {
        var toast = new ToastWindow { _onOpen = onOpen };
        toast.TitleText.Text = title;
        toast.BodyText.Text = body;
        toast.Show();

        DispatcherTimer.RunOnce(() => { try { toast.Close(); } catch { /* already closed */ } }, TimeSpan.FromSeconds(9));
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (screen is not null)
        {
            var area = screen.WorkingArea;
            Position = new PixelPoint(
                area.X + area.Width - (int)Width - 24,
                area.Y + area.Height - (int)Height - 24);
        }
    }

    private void OnOpen(object? sender, RoutedEventArgs e)
    {
        _onOpen?.Invoke();
        Close();
    }
}
