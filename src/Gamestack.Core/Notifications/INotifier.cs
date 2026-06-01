namespace Gamestack.Core.Notifications;

/// <summary>A message to deliver through one or more notification channels.</summary>
/// <param name="Subject">Short subject/headline.</param>
/// <param name="Body">Plain-text body.</param>
/// <param name="RecipientEmail">Target email (used by email channels; channels like Slack may ignore it).</param>
/// <param name="RecipientName">Display name of the recipient, when known.</param>
public sealed record NotificationMessage(string Subject, string Body, string? RecipientEmail, string? RecipientName);

/// <summary>
/// A delivery channel for notifications (e.g. Slack webhook, SMTP email). Implementations live in
/// the infrastructure layer; the app builds the enabled set from settings and sends best-effort.
/// </summary>
public interface INotifier
{
    /// <summary>Human-readable channel name (for status messages), e.g. "Slack".</summary>
    string Name { get; }

    /// <summary>Deliver the message. Throws on delivery failure.</summary>
    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}
