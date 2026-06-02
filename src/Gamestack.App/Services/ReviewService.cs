using Gamestack.Core.Models;
using Gamestack.Core.Notifications;
using Gamestack.Core.Versioning;

namespace Gamestack.App.Services;

/// <summary>
/// Coordinates the review workflow: mutates the manifest (assignment / verdict), persists it via the
/// session's engine, and fans the notification out to every configured channel (best-effort). Returns
/// a short human-readable status describing what happened.
/// </summary>
public sealed class ReviewService
{
    private readonly WorkspaceSession _session;
    private readonly ManifestService _manifests;

    public ReviewService(WorkspaceSession session, ManifestService manifests)
    {
        _session = session;
        _manifests = manifests;
    }

    /// <summary>Assign a reviewer to a version, save the manifest, and notify the reviewer.</summary>
    public async Task<string> RequestReviewAsync(Manifest manifest, string path, int version,
        UserIdentity reviewer, UserIdentity requestedBy, string? note, CancellationToken ct = default)
    {
        _manifests.RequestReview(manifest, path, version, reviewer, requestedBy);
        await _session.Engine!.SaveAssetAsync(_session.ProjectRemoteRoot, manifest, path, ct).ConfigureAwait(false);
        var message = ReviewNotifications.Requested(path, version, reviewer, requestedBy, note);
        return await NotifyAsync(message, ct).ConfigureAwait(false);
    }

    /// <summary>Record a reviewer's verdict, save the manifest, and notify the requester.</summary>
    public async Task<string> SubmitDecisionAsync(Manifest manifest, string path, int version,
        UserIdentity decidedBy, ReviewStatus decision, string comment, UserIdentity requestedBy, CancellationToken ct = default)
    {
        _manifests.SubmitReviewDecision(manifest, path, version, decidedBy, decision, comment);
        await _session.Engine!.SaveAssetAsync(_session.ProjectRemoteRoot, manifest, path, ct).ConfigureAwait(false);
        var message = ReviewNotifications.Decided(path, version, decision, decidedBy, requestedBy, comment);
        return await NotifyAsync(message, ct).ConfigureAwait(false);
    }

    private async Task<string> NotifyAsync(NotificationMessage message, CancellationToken ct)
    {
        if (_session.Notifiers.Count == 0)
            return "Saved. (No notification channels configured.)";

        var sent = new List<string>();
        var failed = new List<string>();
        foreach (var notifier in _session.Notifiers)
        {
            try
            {
                await notifier.SendAsync(message, ct).ConfigureAwait(false);
                sent.Add(notifier.Name);
            }
            catch (Exception ex)
            {
                failed.Add($"{notifier.Name}: {ex.Message}");
            }
        }

        var parts = new List<string>();
        if (sent.Count > 0) parts.Add($"Notified via {string.Join(", ", sent)}.");
        if (failed.Count > 0) parts.Add($"Failed: {string.Join("; ", failed)}.");
        return string.Join(" ", parts);
    }
}
