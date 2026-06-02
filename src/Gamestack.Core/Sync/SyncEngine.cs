using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Versioning;

namespace Gamestack.Core.Sync;

/// <summary>Outcome of a push attempt.</summary>
/// <param name="Conflict">True when the push was refused because the remote advanced past the local baseline.</param>
/// <param name="Version">The created version when the push succeeded; otherwise <c>null</c>.</param>
/// <param name="BaselineVersion">Version the local edit was based on.</param>
/// <param name="RemoteVersion">Current version recorded in the remote manifest.</param>
public sealed record PushResult(bool Conflict, AssetVersion? Version, int BaselineVersion, int RemoteVersion);

/// <summary>
/// Orchestrates sync between the local workspace and an <see cref="IStorageBackend"/>: loads/saves
/// the per-project manifest, downloads files (recording a baseline), and pushes local changes with
/// an auto-assigned version. The engine is backend-agnostic and unit-testable with a fake backend.
/// </summary>
public sealed class SyncEngine
{
    private readonly IStorageBackend _backend;
    private readonly ILocalStateStore _state;
    private readonly ManifestService _manifests;

    /// <summary>Create the engine over a backend, local state store, and manifest service.</summary>
    public SyncEngine(IStorageBackend backend, ILocalStateStore state, ManifestService manifests)
    {
        _backend = backend;
        _state = state;
        _manifests = manifests;
        Metadata = new AssetMetadataStore(backend, manifests);
    }

    /// <summary>The sharded metadata store backing manifest load/save (one file per asset).</summary>
    public AssetMetadataStore Metadata { get; }

    /// <summary>
    /// Load the workspace manifest, assembled from the per-asset shards, returning a fresh empty one
    /// when nothing has been stored yet.
    /// </summary>
    public Task<Manifest> LoadManifestAsync(string projectRemoteRoot, string projectName, CancellationToken ct = default)
        => Metadata.LoadAsync(projectRemoteRoot, projectName, ct);

    /// <summary>Persist a single asset's shard from the in-memory manifest.</summary>
    public Task SaveAssetAsync(string projectRemoteRoot, Manifest manifest, string relativePath, CancellationToken ct = default)
        => Metadata.SaveAssetAsync(projectRemoteRoot, manifest, relativePath, ct);

    /// <summary>Persist the workspace-level metadata (project info, tag vocabulary, attribute definitions).</summary>
    public Task SaveWorkspaceAsync(string projectRemoteRoot, Manifest manifest, CancellationToken ct = default)
        => Metadata.SaveWorkspaceAsync(projectRemoteRoot, manifest, ct);

    /// <summary>
    /// Download one file into the local workspace, mark it materialized, and record its synced
    /// baseline at the manifest's current version.
    /// </summary>
    public async Task DownloadFileAsync(
        string projectRemoteRoot, string localRoot, string relativePath, Manifest manifest,
        IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var localAbs = LocalPath(localRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(localAbs)!);

        await using (var fs = new FileStream(localAbs, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
        {
            await _backend.DownloadAsync(Join(projectRemoteRoot, relativePath), fs, progress, ct).ConfigureAwait(false);
        }

        var sha = await Hasher.Sha256FileAsync(localAbs, ct).ConfigureAwait(false);
        var size = new FileInfo(localAbs).Length;
        var version = manifest.Files.TryGetValue(relativePath, out var entry) ? entry.CurrentVersion : 0;

        await _state.SetMaterializedAsync(relativePath, true, ct).ConfigureAwait(false);
        await _state.SetBaselineAsync(
            new SyncBaseline(relativePath, version, sha, size, File.GetLastWriteTimeUtc(localAbs)), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Push a local file as a new version. Refuses (returns <see cref="PushResult.Conflict"/>) when the
    /// remote manifest has advanced past the local baseline, unless <paramref name="force"/> is set.
    /// On success the manifest is updated and saved, and the local baseline is advanced.
    /// </summary>
    public async Task<PushResult> PushFileAsync(
        string projectRemoteRoot, string localRoot, string relativePath, Manifest manifest,
        string description, UserIdentity pushedBy, bool force = false,
        IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var baseline = await _state.GetBaselineAsync(relativePath, ct).ConfigureAwait(false);
        var baselineVersion = baseline?.Version ?? 0;

        // Re-read THIS asset's shard so the conflict check sees the authoritative remote version,
        // even if a teammate pushed it since this manifest was loaded. Sync the in-memory entry to it.
        var fresh = await Metadata.LoadAssetAsync(projectRemoteRoot, relativePath, ct).ConfigureAwait(false);
        if (fresh is not null)
            manifest.Files[relativePath] = fresh;
        else
            manifest.Files.Remove(relativePath);
        var remoteVersion = fresh?.CurrentVersion ?? 0;

        if (!force && remoteVersion > baselineVersion)
            return new PushResult(Conflict: true, Version: null, baselineVersion, remoteVersion);

        var localAbs = LocalPath(localRoot, relativePath);
        var size = new FileInfo(localAbs).Length;

        RemoteFileInfo info;
        await using (var fs = new FileStream(localAbs, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true))
        {
            info = await _backend.UploadAsync(Join(projectRemoteRoot, relativePath), fs, progress, ct).ConfigureAwait(false);
        }

        var sha = await Hasher.Sha256FileAsync(localAbs, ct).ConfigureAwait(false);
        var version = _manifests.AddVersion(manifest, relativePath, sha, size, pushedBy, description, info.BackendVersionId);
        await SaveAssetAsync(projectRemoteRoot, manifest, relativePath, ct).ConfigureAwait(false);

        await _state.SetBaselineAsync(
            new SyncBaseline(relativePath, version.Version, sha, size, File.GetLastWriteTimeUtc(localAbs)), ct)
            .ConfigureAwait(false);

        return new PushResult(Conflict: false, version, baselineVersion, remoteVersion);
    }

    private static string LocalPath(string localRoot, string relativePath)
        => Path.GetFullPath(Path.Combine(localRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Join(string root, string relative)
    {
        root = root.Trim('/');
        relative = relative.Trim('/');
        return root.Length == 0 ? relative : $"{root}/{relative}";
    }
}
