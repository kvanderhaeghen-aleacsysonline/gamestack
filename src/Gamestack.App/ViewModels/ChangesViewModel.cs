using System.Collections.ObjectModel;
using Gamestack.App.Services;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gamestack.App.ViewModels;

/// <summary>A pending change shown in the Changes list, with its validation badge.</summary>
public sealed class ChangeItemViewModel
{
    public required FileChange Change { get; init; }
    public string Path => Change.Path;
    public string KindLabel => Change.Kind.ToString();
    public string SizeLabel => Change.Kind switch
    {
        ChangeKind.Added => $"+{Change.NewSize} B",
        ChangeKind.Deleted => "deleted",
        _ => $"{Change.OldSize} → {Change.NewSize} B",
    };

    public bool HasWarnings => Change.Warnings.Count > 0;
    public bool HasBlockingError => Change.Warnings.Any(w => w.Severity == ValidationSeverity.Error);
    public string Badge => !HasWarnings ? "" : (HasBlockingError ? "⛔" : "⚠");
    public string WarningSummary => string.Join("; ", Change.Warnings.Select(w => w.Message));
}

/// <summary>Lists files changed in the workspace and pushes them with a description + auto-version.</summary>
public partial class ChangesViewModel : ViewModelBase, IAsyncLoad
{
    private readonly WorkspaceSession _session;
    private readonly IAuthProvider _auth;
    private Manifest _manifest = new() { ProjectId = "" };

    [ObservableProperty] private ChangeItemViewModel? _selectedChange;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _hasConflict;

    public ObservableCollection<ChangeItemViewModel> Changes { get; } = new();

    public ChangesViewModel(WorkspaceSession session, IAuthProvider auth)
    {
        _session = session;
        _auth = auth;
    }

    public async Task LoadAsync()
    {
        if (_session.Engine is null) return;
        _manifest = await _session.Engine.LoadManifestAsync(_session.ProjectRemoteRoot, "Workspace");
        await DetectAsync();
    }

    private async Task DetectAsync()
    {
        Changes.Clear();
        var found = await _session.ChangeDetector.DetectAsync(_session.Settings.WorkspaceRoot!, _session.Settings.Validation);
        foreach (var c in found)
            Changes.Add(new ChangeItemViewModel { Change = c });
        Status = $"{Changes.Count} pending change(s).";
    }

    [RelayCommand] private Task Refresh() => DetectAsync();

    partial void OnSelectedChangeChanged(ChangeItemViewModel? value)
    {
        HasConflict = false;
        PushCommand.NotifyCanExecuteChanged();
    }

    partial void OnDescriptionChanged(string value) => PushCommand.NotifyCanExecuteChanged();

    private bool CanPush =>
        SelectedChange is { Change.Kind: not ChangeKind.Deleted } &&
        !string.IsNullOrWhiteSpace(Description);

    [RelayCommand(CanExecute = nameof(CanPush))]
    private Task Push() => PushInternal(force: false);

    [RelayCommand]
    private Task ForcePush() => PushInternal(force: true);

    private async Task PushInternal(bool force)
    {
        if (SelectedChange is null || _session.Engine is null) return;
        if (_auth.CurrentUser is null) { Status = "No identity resolved — cannot attribute the push."; return; }
        if (SelectedChange.HasBlockingError) { Status = "Blocked: resolve the validation errors first."; return; }

        try
        {
            var result = await _session.Engine.PushFileAsync(
                _session.ProjectRemoteRoot, _session.Settings.WorkspaceRoot!, SelectedChange.Path,
                _manifest, Description.Trim(), _auth.CurrentUser, force);

            if (result.Conflict)
            {
                HasConflict = true;
                Status = $"Conflict: remote is v{result.RemoteVersion} but your edit is based on v{result.BaselineVersion}. Use Force to overwrite.";
                return;
            }

            HasConflict = false;
            Description = "";
            Status = $"Pushed {SelectedChange.Path} as v{result.Version!.Version}.";
            await DetectAsync();
        }
        catch (Exception ex) { Status = $"Push failed: {ex.Message}"; }
    }
}
