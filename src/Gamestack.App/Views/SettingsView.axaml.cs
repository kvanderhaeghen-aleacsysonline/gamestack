using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gamestack.App.ViewModels;

namespace Gamestack.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void OnBrowseSynced(object? sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Select the OneDrive/SharePoint synced folder");
        if (path is not null && DataContext is SettingsViewModel vm)
            vm.SyncedFolderRoot = path;
    }

    private async void OnBrowseWorkspace(object? sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Select your local working folder");
        if (path is not null && DataContext is SettingsViewModel vm)
            vm.WorkspaceRoot = path;
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return null;
        var result = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }
}
