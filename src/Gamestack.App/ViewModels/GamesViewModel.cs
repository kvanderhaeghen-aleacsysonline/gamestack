using System.Collections.ObjectModel;
using Gamestack.App.Services;
using Gamestack.Core.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>One project directory with its editable game id.</summary>
public partial class GameLinkItemViewModel : ObservableObject
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public bool HadMarker { get; init; }

    /// <summary>Indicator shown next to a row: blank when already linked, "new" when it has no marker yet.</summary>
    public string MarkerLabel => HadMarker ? "" : "new";

    [ObservableProperty] private string _gameId = "";
}

/// <summary>
/// Lists the top-level project directories in the synced folder and lets the user edit each one's
/// <c>gameId</c> (stored in that directory's <c>gamestack.json</c>).
/// </summary>
public partial class GamesViewModel : ViewModelBase, IAsyncLoad
{
    private readonly WorkspaceSession _session;
    private readonly GameLinkService _links;
    private readonly DialogService _dialogs;

    [ObservableProperty] private string _status = "";

    public ObservableCollection<GameLinkItemViewModel> Projects { get; } = new();

    public GamesViewModel(WorkspaceSession session, GameLinkService links, DialogService dialogs)
    {
        _session = session;
        _links = links;
        _dialogs = dialogs;
    }

    public async Task LoadAsync()
    {
        Projects.Clear();
        if (_session.Backend is null) return;

        foreach (var dir in await _links.ScanAsync(_session.Backend))
        {
            Projects.Add(new GameLinkItemViewModel
            {
                Name = dir.Name,
                Path = dir.Path,
                HadMarker = dir.Link is not null,
                GameId = dir.Link?.GameId ?? dir.Name,
            });
        }
        Status = $"{Projects.Count} project folder(s).";
    }

    [RelayCommand] private Task Refresh() => LoadAsync();

    [RelayCommand]
    private async Task AddGame()
    {
        if (_session.Backend is null) return;

        var name = await _dialogs.PromptAsync(
            "Add new game",
            "Folder name for the new game. A gamestack.json with this as the gameId will be created in your synced folder.");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            await _links.CreateGameAsync(_session.Backend, name);
            await LoadAsync();
            Status = $"Created game '{name.Trim()}'.";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    private async Task CreateMissing()
    {
        if (_session.Backend is null) return;
        var created = await _links.EnsureMarkersAsync(_session.Backend);
        await LoadAsync();
        Status = created > 0 ? $"Created {created} gamestack.json file(s)." : "All folders already linked.";
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_session.Backend is null) return;
        try
        {
            foreach (var item in Projects)
            {
                var gameId = string.IsNullOrWhiteSpace(item.GameId) ? item.Name : item.GameId.Trim();
                await _links.WriteAsync(_session.Backend, item.Path, new GameLink { GameId = gameId });
            }
            Status = $"Saved {Projects.Count} game link(s).";
        }
        catch (Exception ex) { Status = $"Save failed: {ex.Message}"; }
    }
}
