using System.Collections.ObjectModel;
using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>A review request shown in the inbox.</summary>
public sealed class ReviewItemViewModel
{
    public required string Path { get; init; }
    public required int Version { get; init; }
    public required ReviewRequest Review { get; init; }

    public string Title => $"{Path}  v{Version}";
    public string ReviewerLabel => Review.Reviewer.DisplayName;
    public string RequestedByLabel => Review.RequestedBy.DisplayName;
    public string StatusLabel => Review.Status switch
    {
        ReviewStatus.Pending => "⏳ Pending",
        ReviewStatus.Approved => "✅ Approved",
        ReviewStatus.ChangesRequested => "✋ Changes requested",
        _ => Review.Status.ToString(),
    };
    public bool IsPending => Review.Status == ReviewStatus.Pending;
}

/// <summary>Lists review requests from the manifest and lets the reviewer approve or request changes.</summary>
public partial class ReviewInboxViewModel : ViewModelBase, IAsyncLoad
{
    private readonly WorkspaceSession _session;
    private readonly ReviewService _reviews;
    private readonly IAuthProvider _auth;
    private Manifest _manifest = new() { ProjectId = "" };

    [ObservableProperty] private ReviewItemViewModel? _selected;
    [ObservableProperty] private string _decisionComment = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _mineOnly = true;

    public ObservableCollection<ReviewItemViewModel> Reviews { get; } = new();

    public ReviewInboxViewModel(WorkspaceSession session, ReviewService reviews, IAuthProvider auth)
    {
        _session = session;
        _reviews = reviews;
        _auth = auth;
    }

    public async Task LoadAsync()
    {
        if (_session.Engine is null) return;
        _manifest = await _session.Engine.LoadManifestAsync(_session.ProjectRemoteRoot, "Workspace");
        Rebuild();
    }

    private void Rebuild()
    {
        Reviews.Clear();
        var myEmail = _auth.CurrentUser?.Email;
        foreach (var (path, file) in _manifest.Files)
        {
            foreach (var version in file.Versions)
            {
                if (version.Review is not { } review) continue;
                if (MineOnly && !string.IsNullOrWhiteSpace(myEmail) &&
                    !string.Equals(review.Reviewer.Email, myEmail, StringComparison.OrdinalIgnoreCase))
                    continue;
                Reviews.Add(new ReviewItemViewModel { Path = path, Version = version.Version, Review = review });
            }
        }
        Status = $"{Reviews.Count} review(s){(MineOnly ? " assigned to you" : "")}.";
    }

    [RelayCommand] private Task Refresh() => LoadAsync();

    partial void OnMineOnlyChanged(bool value) => Rebuild();

    partial void OnSelectedChanged(ReviewItemViewModel? value)
    {
        ApproveCommand.NotifyCanExecuteChanged();
        RequestChangesCommand.NotifyCanExecuteChanged();
    }

    private bool CanDecide => Selected is { IsPending: true } && _auth.CurrentUser is not null;

    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task Approve() => DecideAsync(ReviewStatus.Approved);

    [RelayCommand(CanExecute = nameof(CanDecide))]
    private Task RequestChanges() => DecideAsync(ReviewStatus.ChangesRequested);

    private async Task DecideAsync(ReviewStatus decision)
    {
        if (Selected is null || _auth.CurrentUser is null) return;
        try
        {
            Status = await _reviews.SubmitDecisionAsync(
                _manifest, Selected.Path, Selected.Version, _auth.CurrentUser, decision,
                DecisionComment.Trim(), Selected.Review.RequestedBy);
            DecisionComment = "";
            Rebuild();
        }
        catch (Exception ex) { Status = $"Could not submit decision: {ex.Message}"; }
    }
}
