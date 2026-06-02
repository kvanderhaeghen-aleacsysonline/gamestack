using System.Collections.ObjectModel;
using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>A file or folder shown in the explorer.</summary>
public sealed class RemoteEntryViewModel
{
    public required RemoteEntry Entry { get; init; }
    public string Name => Entry.Name;
    public bool IsFolder => Entry.Kind == RemoteEntryKind.Folder;
    public string Path => Entry.Path;
    public string Glyph => IsFolder ? "📁" : "📄";
    public string SizeLabel => IsFolder ? "" : FormatSize(Entry.Size);

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
    };
}

/// <summary>One custom-attribute key/value pair shown for the selected file.</summary>
public sealed class AttributeRowViewModel
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string Display => $"{Key}: {Value}";
}

/// <summary>
/// Browses the synced-folder "remote", downloads selected files into the workspace, and shows the
/// version history + feedback chat for the selected file.
/// </summary>
public partial class ExplorerViewModel : ViewModelBase, IAsyncLoad
{
    private readonly WorkspaceSession _session;
    private readonly ManifestService _manifests;
    private readonly IAuthProvider _auth;
    private readonly ReviewService _reviews;
    private Manifest _manifest = new() { ProjectId = "" };

    [ObservableProperty] private string _currentPath = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private RemoteEntryViewModel? _selectedItem;
    [ObservableProperty] private string _selectedFileLabel = "";
    [ObservableProperty] private string _newComment = "";
    [ObservableProperty] private string _reviewerEmail = "";
    [ObservableProperty] private string _reviewerName = "";
    [ObservableProperty] private string _reviewNote = "";
    [ObservableProperty] private string _reviewStatusLabel = "";
    [ObservableProperty] private string _newTag = "";
    [ObservableProperty] private string _newAttributeKey = "";
    [ObservableProperty] private string _newAttributeValue = "";

    public ObservableCollection<RemoteEntryViewModel> Items { get; } = new();
    public ObservableCollection<AssetVersion> Versions { get; } = new();
    public ObservableCollection<Comment> Comments { get; } = new();

    /// <summary>Tags assigned to the selected file.</summary>
    public ObservableCollection<string> Tags { get; } = new();

    /// <summary>Custom attribute values (key → value) on the selected file.</summary>
    public ObservableCollection<AttributeRowViewModel> Attributes { get; } = new();

    public ExplorerViewModel(WorkspaceSession session, ManifestService manifests, IAuthProvider auth, ReviewService reviews)
    {
        _session = session;
        _manifests = manifests;
        _auth = auth;
        _reviews = reviews;
    }

    public async Task LoadAsync()
    {
        if (_session.Engine is null) return;
        _manifest = await _session.Engine.LoadManifestAsync(_session.ProjectRemoteRoot, "Workspace");
        await ListAsync(CurrentPath);
    }

    private async Task ListAsync(string path)
    {
        if (_session.Backend is null) return;
        Items.Clear();
        try
        {
            var entries = await _session.Backend.ListAsync(path);
            foreach (var e in entries
                         .Where(e => !e.Name.Equals(".gamestack", StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(e => e.Kind == RemoteEntryKind.Folder)
                         .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                Items.Add(new RemoteEntryViewModel { Entry = e });
            }
            CurrentPath = path;
            Status = $"{Items.Count} item(s) in /{path}";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    private async Task Open(RemoteEntryViewModel? item)
    {
        if (item is { IsFolder: true })
            await ListAsync(item.Path);
    }

    [RelayCommand]
    private async Task NavigateUp()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;
        var idx = CurrentPath.LastIndexOf('/');
        await ListAsync(idx < 0 ? "" : CurrentPath[..idx]);
    }

    [RelayCommand] private Task Refresh() => ListAsync(CurrentPath);

    [RelayCommand]
    private async Task Download(RemoteEntryViewModel? item)
    {
        if (item is null || _session.Engine is null || _session.Backend is null) return;
        try
        {
            if (item.IsFolder)
            {
                Status = $"Downloading folder {item.Name}…";
                var count = await DownloadFolderAsync(item.Path);
                Status = $"Downloaded {count} file(s) from {item.Name} to workspace.";
            }
            else
            {
                Status = $"Downloading {item.Name}…";
                await _session.Engine.DownloadFileAsync(_session.ProjectRemoteRoot, _session.Settings.WorkspaceRoot!, item.Path, _manifest);
                Status = $"Downloaded {item.Name} to workspace.";
            }
        }
        catch (Exception ex) { Status = $"Download failed: {ex.Message}"; }
    }

    /// <summary>Recursively download every file under a remote folder into the workspace.</summary>
    private async Task<int> DownloadFolderAsync(string folderPath)
    {
        var count = 0;
        var pending = new Stack<string>();
        pending.Push(folderPath);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            foreach (var entry in await _session.Backend!.ListAsync(dir))
            {
                if (entry.Name.Equals(".gamestack", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.Kind == RemoteEntryKind.Folder)
                {
                    pending.Push(entry.Path);
                }
                else
                {
                    await _session.Engine!.DownloadFileAsync(_session.ProjectRemoteRoot, _session.Settings.WorkspaceRoot!, entry.Path, _manifest);
                    count++;
                    Status = $"Downloaded {count} file(s)…";
                }
            }
        }
        return count;
    }

    partial void OnSelectedItemChanged(RemoteEntryViewModel? value)
    {
        Versions.Clear();
        Comments.Clear();
        Tags.Clear();
        Attributes.Clear();
        SelectedFileLabel = "";
        ReviewStatusLabel = "";
        PostCommentCommand.NotifyCanExecuteChanged();
        RequestReviewCommand.NotifyCanExecuteChanged();
        AddTagCommand.NotifyCanExecuteChanged();
        SetAttributeCommand.NotifyCanExecuteChanged();
        if (value is null || value.IsFolder) return;

        SelectedFileLabel = value.Path;
        if (_manifest.Files.TryGetValue(value.Path, out var file))
        {
            foreach (var v in file.Versions.OrderByDescending(v => v.Version)) Versions.Add(v);
            foreach (var c in file.Comments) Comments.Add(c);
            RefreshOrganization(file);

            var latest = file.Versions.LastOrDefault();
            ReviewStatusLabel = latest?.Review is { } r
                ? $"v{latest.Version} review: {r.Status} (reviewer {r.Reviewer.DisplayName})"
                : "No review on the latest version.";
        }
        else
        {
            ReviewStatusLabel = "Not yet pushed — no version to review.";
        }
    }

    private void RefreshOrganization(AssetFile file)
    {
        Tags.Clear();
        Attributes.Clear();
        foreach (var t in file.Tags) Tags.Add(t);
        foreach (var (k, v) in file.Attributes) Attributes.Add(new AttributeRowViewModel { Key = k, Value = v });
    }

    private bool CanEditSelectedFile => SelectedItem is { IsFolder: false };

    private bool CanAddTag => CanEditSelectedFile && !string.IsNullOrWhiteSpace(NewTag);
    partial void OnNewTagChanged(string value) => AddTagCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanAddTag))]
    private async Task AddTag()
    {
        if (SelectedItem is null || _session.Engine is null) return;
        try
        {
            if (_manifests.AddFileTag(_manifest, SelectedItem.Path, NewTag))
            {
                await _session.Engine.SaveManifestAsync(_session.ProjectRemoteRoot, _manifest);
                RefreshOrganization(_manifest.Files[SelectedItem.Path]);
            }
            NewTag = "";
        }
        catch (Exception ex) { Status = $"Could not add tag: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task RemoveTag(string? tag)
    {
        if (tag is null || SelectedItem is null || _session.Engine is null) return;
        try
        {
            if (_manifests.RemoveFileTag(_manifest, SelectedItem.Path, tag))
            {
                await _session.Engine.SaveManifestAsync(_session.ProjectRemoteRoot, _manifest);
                if (_manifest.Files.TryGetValue(SelectedItem.Path, out var f)) RefreshOrganization(f);
            }
        }
        catch (Exception ex) { Status = $"Could not remove tag: {ex.Message}"; }
    }

    private bool CanSetAttribute => CanEditSelectedFile && !string.IsNullOrWhiteSpace(NewAttributeKey);
    partial void OnNewAttributeKeyChanged(string value) => SetAttributeCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSetAttribute))]
    private async Task SetAttribute()
    {
        if (SelectedItem is null || _session.Engine is null) return;
        try
        {
            _manifests.SetAttribute(_manifest, SelectedItem.Path, NewAttributeKey.Trim(), NewAttributeValue.Trim());
            await _session.Engine.SaveManifestAsync(_session.ProjectRemoteRoot, _manifest);
            if (_manifest.Files.TryGetValue(SelectedItem.Path, out var f)) RefreshOrganization(f);
            NewAttributeKey = "";
            NewAttributeValue = "";
        }
        catch (Exception ex) { Status = $"Could not set attribute: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task RemoveAttribute(string? key)
    {
        if (key is null || SelectedItem is null || _session.Engine is null) return;
        try
        {
            _manifests.SetAttribute(_manifest, SelectedItem.Path, key, null);
            await _session.Engine.SaveManifestAsync(_session.ProjectRemoteRoot, _manifest);
            if (_manifest.Files.TryGetValue(SelectedItem.Path, out var f)) RefreshOrganization(f);
        }
        catch (Exception ex) { Status = $"Could not remove attribute: {ex.Message}"; }
    }

    private bool CanPostComment => SelectedItem is { IsFolder: false } && !string.IsNullOrWhiteSpace(NewComment);
    partial void OnNewCommentChanged(string value) => PostCommentCommand.NotifyCanExecuteChanged();

    private bool CanRequestReview =>
        SelectedItem is { IsFolder: false } item &&
        !string.IsNullOrWhiteSpace(ReviewerEmail) &&
        _auth.CurrentUser is not null &&
        _manifest.Files.TryGetValue(item.Path, out var f) && f.CurrentVersion > 0;

    partial void OnReviewerEmailChanged(string value) => RequestReviewCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanRequestReview))]
    private async Task RequestReview()
    {
        if (SelectedItem is null || _auth.CurrentUser is null) return;
        if (!_manifest.Files.TryGetValue(SelectedItem.Path, out var file) || file.CurrentVersion == 0)
        {
            Status = "This file hasn't been pushed yet, so there's no version to review.";
            return;
        }
        try
        {
            var reviewer = new Core.Models.UserIdentity(
                ReviewerEmail.Trim(),
                string.IsNullOrWhiteSpace(ReviewerName) ? ReviewerEmail.Trim() : ReviewerName.Trim(),
                ReviewerEmail.Trim());
            Status = await _reviews.RequestReviewAsync(
                _manifest, SelectedItem.Path, file.CurrentVersion, reviewer, _auth.CurrentUser, ReviewNote);
            ReviewNote = "";
            // refresh the detail panel so the new review status shows
            OnSelectedItemChanged(SelectedItem);
        }
        catch (Exception ex) { Status = $"Could not request review: {ex.Message}"; }
    }

    [RelayCommand(CanExecute = nameof(CanPostComment))]
    private async Task PostComment()
    {
        if (SelectedItem is null || _session.Engine is null || _auth.CurrentUser is null) return;
        try
        {
            var version = _manifest.Files.TryGetValue(SelectedItem.Path, out var f) ? f.CurrentVersion : 0;
            var comment = _manifests.AddComment(_manifest, SelectedItem.Path, _auth.CurrentUser, NewComment.Trim(), version);
            await _session.Engine.SaveManifestAsync(_session.ProjectRemoteRoot, _manifest);
            Comments.Add(comment);
            NewComment = "";
        }
        catch (Exception ex) { Status = $"Could not post comment: {ex.Message}"; }
    }
}
