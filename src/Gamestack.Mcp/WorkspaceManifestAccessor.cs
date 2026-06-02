using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Settings;
using Gamestack.Core.Sync;
using Gamestack.Core.Versioning;
using Gamestack.Storage.SyncedFolder;

namespace Gamestack.Mcp;

/// <summary>
/// Loads the workspace <see cref="Manifest"/> for the MCP tools, reusing the same settings file and
/// synced-folder layout as the desktop app. Read-only: it never writes the manifest or settings.
/// </summary>
public sealed class WorkspaceManifestAccessor
{
    private readonly ISettingsStore _settings;
    private readonly ManifestService _manifests;

    /// <summary>Create the accessor over the app's settings store.</summary>
    public WorkspaceManifestAccessor(ISettingsStore settings, ManifestService manifests)
    {
        _settings = settings;
        _manifests = manifests;
    }

    /// <summary>
    /// Load the current workspace manifest. Throws <see cref="InvalidOperationException"/> when no
    /// synced-folder root has been configured (the desktop app hasn't completed setup).
    /// </summary>
    public async Task<Manifest> LoadManifestAsync(CancellationToken ct = default)
    {
        var settings = await _settings.LoadAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(settings.SyncedFolderRoot))
            throw new InvalidOperationException(
                "Gamestack is not configured yet — no synced-folder root found in settings. " +
                "Complete setup in the Gamestack desktop app first.");

        var backend = new SyncedFolderBackend(settings.SyncedFolderRoot!);
        var store = new AssetMetadataStore(backend, _manifests);
        return await store.LoadAsync("", "Workspace", ct).ConfigureAwait(false);
    }
}
