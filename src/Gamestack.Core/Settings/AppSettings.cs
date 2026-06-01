using Gamestack.Core.Validation;

namespace Gamestack.Core.Settings;

/// <summary>A project folder the user has linked, with its game association.</summary>
public sealed class ProjectLink
{
    /// <summary>Display name of the project.</summary>
    public required string Name { get; set; }

    /// <summary>Project root path relative to <see cref="AppSettings.SyncedFolderRoot"/> ('/'-separated).</summary>
    public required string RemotePath { get; set; }

    /// <summary>Linked game slug (e.g. <c>cosmic-slots</c>), if any.</summary>
    public string? GameSlug { get; set; }

    /// <summary>Linked game id, if any.</summary>
    public string? GameId { get; set; }
}

/// <summary>
/// All user-configurable application settings, persisted as JSON. "Everything that could be a
/// setting" lives here so the Settings page can grow without new plumbing.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Absolute path to the OneDrive/SharePoint sync-client folder used as the "remote".</summary>
    public string? SyncedFolderRoot { get; set; }

    /// <summary>Absolute path to the local working directory where artists edit files.</summary>
    public string? WorkspaceRoot { get; set; }

    /// <summary>Asset validation rules and their thresholds.</summary>
    public ValidationSettings Validation { get; set; } = new();

    /// <summary>Outbound notification channel configuration for review requests/responses.</summary>
    public NotificationSettings Notifications { get; set; } = new();

    /// <summary>Start the app automatically when the user logs in (Windows).</summary>
    public bool RunOnStartup { get; set; }

    /// <summary>Attempt to delay Windows shutdown/restart while unpushed changes exist (Windows).</summary>
    public bool HoldShutdownWithUnpushedChanges { get; set; }

    /// <summary>Show an end-of-day reminder listing files changed but not yet pushed.</summary>
    public bool EndOfDayReminderEnabled { get; set; }

    /// <summary>Time of day for the end-of-day reminder, as <c>HH:mm</c>.</summary>
    public string EndOfDayReminderTime { get; set; } = "17:30";

    /// <summary>Projects the user has linked locally.</summary>
    public List<ProjectLink> Projects { get; set; } = new();
}

/// <summary>
/// Configuration for outbound review notifications. Each channel is independently toggleable.
/// Note: <see cref="SmtpPassword"/> is stored as plain text in settings.json — acceptable for a
/// per-user machine-local file, but prefer an app password / relay over a primary credential.
/// </summary>
public sealed class NotificationSettings
{
    /// <summary>Send notifications to a Slack channel via an Incoming Webhook.</summary>
    public bool SlackEnabled { get; set; }

    /// <summary>Slack Incoming Webhook URL.</summary>
    public string? SlackWebhookUrl { get; set; }

    /// <summary>Send notifications by email over SMTP.</summary>
    public bool EmailEnabled { get; set; }

    /// <summary>SMTP server host name.</summary>
    public string? SmtpHost { get; set; }

    /// <summary>SMTP server port (587 for STARTTLS is typical).</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>SMTP username (often the From mailbox).</summary>
    public string? SmtpUser { get; set; }

    /// <summary>SMTP password / app password.</summary>
    public string? SmtpPassword { get; set; }

    /// <summary>From address; defaults to <see cref="SmtpUser"/> when empty.</summary>
    public string? FromAddress { get; set; }

    /// <summary>Use STARTTLS (true) versus automatic/SSL negotiation.</summary>
    public bool SmtpUseStartTls { get; set; } = true;
}
