using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>
/// First-run setup: pick the OneDrive/SharePoint synced folder (the "remote") and a local working
/// directory, confirm the detected identity, then persist and continue.
/// </summary>
public partial class SetupViewModel : ViewModelBase
{
    private readonly ISettingsStore _settingsStore;
    private readonly INavigator _navigator;

    [ObservableProperty] private string? _syncedFolderRoot;
    [ObservableProperty] private string? _workspaceRoot;
    [ObservableProperty] private string _identityLabel;
    [ObservableProperty] private string _status = "";

    public SetupViewModel(ISettingsStore settingsStore, INavigator navigator, IAuthProvider auth)
    {
        _settingsStore = settingsStore;
        _navigator = navigator;
        _identityLabel = auth.CurrentUser is { } u
            ? (string.IsNullOrWhiteSpace(u.Email) ? u.DisplayName : $"{u.DisplayName} <{u.Email}>")
            : "No OneDrive/Windows identity detected";
    }

    private bool CanContinue =>
        !string.IsNullOrWhiteSpace(SyncedFolderRoot) && Directory.Exists(SyncedFolderRoot) &&
        !string.IsNullOrWhiteSpace(WorkspaceRoot);

    partial void OnSyncedFolderRootChanged(string? value) => ContinueCommand.NotifyCanExecuteChanged();
    partial void OnWorkspaceRootChanged(string? value) => ContinueCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private async Task Continue()
    {
        try
        {
            Directory.CreateDirectory(WorkspaceRoot!);
            var settings = await _settingsStore.LoadAsync();
            settings.SyncedFolderRoot = SyncedFolderRoot;
            settings.WorkspaceRoot = WorkspaceRoot;
            await _settingsStore.SaveAsync(settings);
            await _navigator.ApplyConfigurationAsync();
        }
        catch (Exception ex)
        {
            Status = $"Could not save setup: {ex.Message}";
        }
    }
}
