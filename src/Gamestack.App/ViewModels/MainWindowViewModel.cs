using System.IO;
using Avalonia.Threading;
using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestack.App.ViewModels;

/// <summary>Application shell: resolves the signed-in identity, owns navigation, and hosts pages.</summary>
public partial class MainWindowViewModel : ViewModelBase, INavigator
{
    private readonly IServiceProvider _services;
    private readonly ISettingsStore _settingsStore;
    private readonly WorkspaceSession _session;
    private readonly IAuthProvider _auth;
    private readonly IStartupService _startup;
    private readonly GameLinkService _links;
    private readonly DialogService _dialogs;

    private FileSystemWatcher? _projectWatcher;
    private DispatcherTimer? _watchDebounce;

    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private bool _isConfigured;
    [ObservableProperty] private string _identityLabel = "Not signed in";

    public MainWindowViewModel(IServiceProvider services, ISettingsStore settingsStore, WorkspaceSession session,
        IAuthProvider auth, IStartupService startup, GameLinkService links, DialogService dialogs)
    {
        _services = services;
        _settingsStore = settingsStore;
        _session = session;
        _auth = auth;
        _startup = startup;
        _links = links;
        _dialogs = dialogs;
    }

    /// <summary>Resolve identity, load settings, and show the first appropriate page.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _auth.SignInAsync();
            if (_auth.CurrentUser is { } user)
                IdentityLabel = string.IsNullOrWhiteSpace(user.Email) ? user.DisplayName : $"{user.DisplayName} <{user.Email}>";
        }
        catch { /* identity is best-effort; app still works */ }

        _session.Configure(await _settingsStore.LoadAsync());
        IsConfigured = _session.IsConfigured;
        _startup.SetEnabled(_session.Settings.RunOnStartup);
        StartProjectWatcher();

        if (IsConfigured)
            await GoToExplorerAsync();
        else
            CurrentPage = _services.GetRequiredService<SetupViewModel>();
    }

    [RelayCommand] private Task GoToGames() => ShowAsync(_services.GetRequiredService<GamesViewModel>());
    [RelayCommand] private Task GoToSearch() => ShowAsync(_services.GetRequiredService<SearchViewModel>());
    [RelayCommand] private Task GoToReviews() => ShowAsync(_services.GetRequiredService<ReviewInboxViewModel>());
    [RelayCommand] private Task GoToExplorer() => GoToExplorerAsync();
    [RelayCommand] private Task GoToChanges() => GoToChangesAsync();
    [RelayCommand] private void OpenSettings() => GoToSettings();

    public Task GoToExplorerAsync() => ShowAsync(_services.GetRequiredService<ExplorerViewModel>());

    public Task GoToChangesAsync() => ShowAsync(_services.GetRequiredService<ChangesViewModel>());

    public void GoToSettings() => CurrentPage = _services.GetRequiredService<SettingsViewModel>();

    public async Task ApplyConfigurationAsync()
    {
        _session.Configure(await _settingsStore.LoadAsync());
        IsConfigured = _session.IsConfigured;
        _startup.SetEnabled(_session.Settings.RunOnStartup);

        // Choosing/changing the synced folder seeds a gamestack.json in every top-level directory.
        if (IsConfigured && _session.Backend is not null)
            await _links.EnsureMarkersAsync(_session.Backend);

        StartProjectWatcher();

        if (IsConfigured)
            await GoToExplorerAsync();
    }

    /// <summary>
    /// On a returning launch, if some project folders have no <c>gamestack.json</c> yet (e.g. new
    /// folders were added in OneDrive), offer to create them. Call once the window is shown.
    /// </summary>
    public async Task CheckProjectMarkersAsync()
    {
        if (!IsConfigured || _session.Backend is null)
            return;

        var missing = await _links.CountMissingAsync(_session.Backend);
        if (missing == 0)
            return;

        var ok = await _dialogs.ConfirmAsync(
            "Link game IDs",
            $"{missing} project folder(s) don't have a gamestack.json yet. Create them now? " +
            "(gameId defaults to the folder name and is editable under Games.)");
        if (ok)
            await _links.EnsureMarkersAsync(_session.Backend);
    }

    /// <summary>
    /// Watch the synced-folder root for new top-level directories while the app runs, so a folder
    /// added (or synced in from OneDrive) live also triggers the gamestack.json prompt. Rebuilt
    /// whenever the configuration changes.
    /// </summary>
    private void StartProjectWatcher()
    {
        _projectWatcher?.Dispose();
        _projectWatcher = null;

        var root = _session.Settings.SyncedFolderRoot;
        if (!IsConfigured || string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return;

        _watchDebounce ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _watchDebounce.Tick -= OnWatchDebounceTick;
        _watchDebounce.Tick += OnWatchDebounceTick;

        var watcher = new FileSystemWatcher(root)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
        };
        watcher.Created += (_, _) => Dispatcher.UIThread.Post(ScheduleProjectCheck);
        watcher.EnableRaisingEvents = true;
        _projectWatcher = watcher;
    }

    private void ScheduleProjectCheck()
    {
        // Collapse a burst of filesystem events into a single check.
        _watchDebounce?.Stop();
        _watchDebounce?.Start();
    }

    private async void OnWatchDebounceTick(object? sender, EventArgs e)
    {
        _watchDebounce?.Stop();
        await CheckProjectMarkersAsync();
    }

    private async Task ShowAsync(ViewModelBase page)
    {
        CurrentPage = page;
        if (page is IAsyncLoad loadable)
            await loadable.LoadAsync();
    }
}
