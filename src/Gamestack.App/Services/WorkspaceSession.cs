using Gamestack.Core.Abstractions;
using Gamestack.Core.Notifications;
using Gamestack.Core.Settings;
using Gamestack.Core.Sync;
using Gamestack.Core.Validation;
using Gamestack.Core.Versioning;
using Gamestack.Infrastructure;
using Gamestack.Storage.SyncedFolder;

namespace Gamestack.App.Services;

/// <summary>
/// Holds the active configuration and the engine objects built from it. Rebuilt whenever settings
/// change (e.g. after setup). For the lean MVP the whole synced-folder root is treated as one
/// project, so the project remote root is the empty string and there is a single root manifest.
/// </summary>
public sealed class WorkspaceSession
{
    private static readonly HttpClient SharedHttp = new();

    private readonly ILocalStateStore _state;
    private readonly AssetValidationRunner _validation;
    private readonly ManifestService _manifests;

    public WorkspaceSession(ILocalStateStore state, AssetValidationRunner validation, ManifestService manifests)
    {
        _state = state;
        _validation = validation;
        _manifests = manifests;
        ChangeDetector = new ChangeDetector(state, validation);
    }

    /// <summary>The current settings.</summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>Change detector over the workspace (independent of the chosen backend).</summary>
    public ChangeDetector ChangeDetector { get; }

    /// <summary>The active backend, or null until a synced-folder root is configured.</summary>
    public IStorageBackend? Backend { get; private set; }

    /// <summary>The active sync engine, or null until configured.</summary>
    public SyncEngine? Engine { get; private set; }

    /// <summary>The enabled notification channels, rebuilt from settings on each <see cref="Configure"/>.</summary>
    public IReadOnlyList<INotifier> Notifiers { get; private set; } = Array.Empty<INotifier>();

    /// <summary>Project remote root for the lean single-project MVP (the synced-folder root itself).</summary>
    public string ProjectRemoteRoot => "";

    /// <summary>True when both a synced-folder root and a workspace root are set.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Settings.SyncedFolderRoot) &&
        !string.IsNullOrWhiteSpace(Settings.WorkspaceRoot) &&
        Engine is not null;

    /// <summary>Apply new settings and (re)build the backend + engine.</summary>
    public void Configure(AppSettings settings)
    {
        Settings = settings;
        if (!string.IsNullOrWhiteSpace(settings.SyncedFolderRoot))
        {
            Backend = new SyncedFolderBackend(settings.SyncedFolderRoot!);
            Engine = new SyncEngine(Backend, _state, _manifests);
        }
        else
        {
            Backend = null;
            Engine = null;
        }

        Notifiers = BuildNotifiers(settings.Notifications);
    }

    private static IReadOnlyList<INotifier> BuildNotifiers(NotificationSettings n)
    {
        var notifiers = new List<INotifier>();
        if (n.SlackEnabled && !string.IsNullOrWhiteSpace(n.SlackWebhookUrl))
            notifiers.Add(new SlackWebhookNotifier(n.SlackWebhookUrl!, SharedHttp));
        if (n.EmailEnabled && !string.IsNullOrWhiteSpace(n.SmtpHost))
            notifiers.Add(new SmtpEmailNotifier(n));
        return notifiers;
    }
}
