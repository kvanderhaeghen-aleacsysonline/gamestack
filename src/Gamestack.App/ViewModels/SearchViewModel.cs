using System.Collections.ObjectModel;
using Gamestack.App.Services;
using Gamestack.Core.Models;
using Gamestack.Core.Organization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>One asset matched by a search, shown in the results list.</summary>
public sealed class SearchResultViewModel
{
    public required string Path { get; init; }
    public required string TagsLabel { get; init; }
    public required int Version { get; init; }
    public string VersionLabel => Version > 0 ? $"v{Version}" : "—";
}

/// <summary>
/// Finds assets across the workspace manifest by name, tag, file type, game id, and custom
/// attribute (see <see cref="AssetSearch"/>).
/// </summary>
public partial class SearchViewModel : ViewModelBase, IAsyncLoad
{
    private readonly WorkspaceSession _session;
    private Manifest _manifest = new() { ProjectId = "" };

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _tagFilter = "";
    [ObservableProperty] private string _extension = "";
    [ObservableProperty] private string _gameId = "";
    [ObservableProperty] private string _attributeKey = "";
    [ObservableProperty] private string _attributeValue = "";
    [ObservableProperty] private string _status = "";

    public ObservableCollection<SearchResultViewModel> Results { get; } = new();

    /// <summary>All tags in the workspace vocabulary, offered as filter hints.</summary>
    public ObservableCollection<string> AllTags { get; } = new();

    public SearchViewModel(WorkspaceSession session) => _session = session;

    public async Task LoadAsync()
    {
        if (_session.Engine is null) return;
        _manifest = await _session.Engine.LoadManifestAsync(_session.ProjectRemoteRoot, "Workspace");
        AllTags.Clear();
        foreach (var t in _manifest.Tags) AllTags.Add(t);
        RunSearch();
    }

    partial void OnSearchTextChanged(string value) => RunSearch();
    partial void OnTagFilterChanged(string value) => RunSearch();
    partial void OnExtensionChanged(string value) => RunSearch();
    partial void OnGameIdChanged(string value) => RunSearch();
    partial void OnAttributeKeyChanged(string value) => RunSearch();
    partial void OnAttributeValueChanged(string value) => RunSearch();

    [RelayCommand] private void Search() => RunSearch();

    private void RunSearch()
    {
        var tags = TagFilter
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        var attributes = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(AttributeKey) && !string.IsNullOrWhiteSpace(AttributeValue))
            attributes[AttributeKey.Trim()] = AttributeValue.Trim();

        var query = new SearchQuery
        {
            Text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            Tags = tags,
            Extension = string.IsNullOrWhiteSpace(Extension) ? null : Extension.Trim(),
            GameId = string.IsNullOrWhiteSpace(GameId) ? null : GameId.Trim(),
            Attributes = attributes,
        };

        Results.Clear();
        foreach (var r in AssetSearch.Search(_manifest, query))
        {
            Results.Add(new SearchResultViewModel
            {
                Path = r.Path,
                TagsLabel = r.File.Tags.Count > 0 ? string.Join(", ", r.File.Tags) : "",
                Version = r.File.CurrentVersion,
            });
        }
        Status = query.IsEmpty
            ? $"{Results.Count} asset(s) tracked."
            : $"{Results.Count} match(es).";
    }
}
