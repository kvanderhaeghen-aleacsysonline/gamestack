using System;
using Avalonia.Controls;
using Gamestack.App.ViewModels;

namespace Gamestack.App.Views;

public partial class MainWindow : Window
{
    private bool _started;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    // Run startup once the window is actually shown, so modal dialogs (e.g. the gamestack.json
    // prompt) have a valid owner. Guarded so re-showing from the tray doesn't re-initialize.
    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_started) return;
        _started = true;

        if (DataContext is MainWindowViewModel vm)
        {
            await vm.InitializeAsync();
            await vm.CheckProjectMarkersAsync();
        }
    }
}
