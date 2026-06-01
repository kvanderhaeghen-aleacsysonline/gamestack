using Gamestack.Core.Models;

namespace Gamestack.Core.Notifications;

/// <summary>Builds the notification messages sent when reviews are requested and answered.</summary>
public static class ReviewNotifications
{
    /// <summary>Message sent to the reviewer when a review is requested.</summary>
    public static NotificationMessage Requested(string path, int version, UserIdentity reviewer, UserIdentity requestedBy, string? note)
    {
        var body = $"{requestedBy.DisplayName} asked you to review:\n  {path}  (version {version})";
        if (!string.IsNullOrWhiteSpace(note))
            body += $"\n\n\"{note.Trim()}\"";
        return new NotificationMessage($"Review requested: {path} v{version}", body, reviewer.Email, reviewer.DisplayName);
    }

    /// <summary>Message sent to the requester when the reviewer responds.</summary>
    public static NotificationMessage Decided(string path, int version, ReviewStatus status, UserIdentity decidedBy, UserIdentity requestedBy, string? comment)
    {
        var body = $"{decidedBy.DisplayName} {Verb(status)}:\n  {path}  (version {version})";
        if (!string.IsNullOrWhiteSpace(comment))
            body += $"\n\n\"{comment.Trim()}\"";
        return new NotificationMessage($"Review {Word(status)}: {path} v{version}", body, requestedBy.Email, requestedBy.DisplayName);
    }

    private static string Verb(ReviewStatus s) => s == ReviewStatus.Approved ? "approved" : "requested changes on";
    private static string Word(ReviewStatus s) => s == ReviewStatus.Approved ? "approved" : "changes requested";
}
