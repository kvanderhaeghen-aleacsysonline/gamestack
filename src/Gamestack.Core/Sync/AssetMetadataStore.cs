using System.Text.Json;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Versioning;

namespace Gamestack.Core.Sync;

/// <summary>
/// Persists workspace metadata as <b>sharded</b> files under <c>.gamestack/</c>: workspace-wide data
/// in <c>workspace.json</c> and one file per asset under <c>files/&lt;path&gt;.json</c>. Sharding means a
/// push rewrites only the affected asset's small file, so concurrent pushes to different assets over a
/// OneDrive synced folder never clobber each other (the single-manifest model rewrote everything).
/// </summary>
/// <remarks>
/// An in-session cache keyed by each shard's last-modified time lets repeated <see cref="LoadAsync"/>
/// calls skip re-reading unchanged shards — a lightweight local index. A legacy single
/// <c>manifest.json</c> is migrated to shards automatically on first load.
/// </remarks>
public sealed class AssetMetadataStore
{
    private const string Meta = ChangeDetector.MetadataFolder; // ".gamestack"

    private readonly IStorageBackend _backend;
    private readonly ManifestService _manifests;

    // Cache: shard remote path -> (mtime when read, parsed shard). Skips re-reading unchanged shards.
    private readonly Dictionary<string, (DateTimeOffset? MTime, AssetShard Shard)> _cache = new(StringComparer.Ordinal);

    /// <summary>Create the store over a storage backend and the manifest (de)serializer.</summary>
    public AssetMetadataStore(IStorageBackend backend, ManifestService manifests)
    {
        _backend = backend;
        _manifests = manifests;
    }

    /// <summary>Remote path of the workspace-level metadata file.</summary>
    public static string WorkspacePath(string root) => Join(root, $"{Meta}/workspace.json");

    /// <summary>Remote path of the legacy single manifest (pre-sharding), used for migration.</summary>
    public static string LegacyManifestPath(string root) => Join(root, $"{Meta}/manifest.json");

    /// <summary>Remote folder holding the per-asset shard files.</summary>
    public static string FilesRoot(string root) => Join(root, $"{Meta}/files");

    /// <summary>Remote path of one asset's shard file.</summary>
    public static string ShardPath(string root, string relativePath)
        => Join(root, $"{Meta}/files/{relativePath.Trim('/')}.json");

    /// <summary>
    /// Assemble the in-memory <see cref="Manifest"/> from the workspace file and all asset shards,
    /// returning a fresh empty manifest when nothing has been stored yet. Migrates a legacy
    /// <c>manifest.json</c> to shards on first encounter.
    /// </summary>
    public async Task<Manifest> LoadAsync(string root, string projectName, CancellationToken ct = default)
    {
        var wsJson = await _backend.ReadTextAsync(WorkspacePath(root), ct).ConfigureAwait(false);
        if (wsJson is null)
        {
            var legacy = await _backend.ReadTextAsync(LegacyManifestPath(root), ct).ConfigureAwait(false);
            if (legacy is not null)
            {
                await MigrateLegacyAsync(root, legacy, ct).ConfigureAwait(false);
                wsJson = await _backend.ReadTextAsync(WorkspacePath(root), ct).ConfigureAwait(false);
            }
        }

        Manifest manifest;
        if (wsJson is null)
        {
            manifest = _manifests.CreateNew(projectName);
        }
        else
        {
            var ws = Deserialize<WorkspaceMetadata>(wsJson);
            manifest = new Manifest
            {
                ProjectId = ws.ProjectId,
                Name = ws.Name,
                GameSlug = ws.GameSlug,
                GameId = ws.GameId,
                Tags = ws.Tags,
                AttributeDefinitions = ws.AttributeDefinitions,
            };
        }

        foreach (var (remotePath, mtime) in await ListShardFilesAsync(root, ct).ConfigureAwait(false))
        {
            var shard = await ReadShardCachedAsync(remotePath, mtime, ct).ConfigureAwait(false);
            if (shard is not null)
                manifest.Files[shard.Path] = shard.File;
        }

        return manifest;
    }

    /// <summary>Load a single asset's record, or <c>null</c> when no shard exists yet.</summary>
    public async Task<AssetFile?> LoadAssetAsync(string root, string relativePath, CancellationToken ct = default)
    {
        var json = await _backend.ReadTextAsync(ShardPath(root, relativePath), ct).ConfigureAwait(false);
        return json is null ? null : Deserialize<AssetShard>(json).File;
    }

    /// <summary>Persist one asset's shard from the in-memory manifest. No-op if the path isn't tracked.</summary>
    public async Task SaveAssetAsync(string root, Manifest manifest, string relativePath, CancellationToken ct = default)
    {
        if (!manifest.Files.TryGetValue(relativePath, out var file))
            return;
        var remotePath = ShardPath(root, relativePath);
        var shard = new AssetShard { Path = relativePath, File = file };
        await _backend.WriteTextAsync(remotePath, Serialize(shard), ct).ConfigureAwait(false);
        _cache[remotePath] = (null, shard); // invalidate mtime so the next load re-reads authoritative bytes
    }

    /// <summary>Persist the workspace-level metadata (project info, tag vocabulary, attribute definitions).</summary>
    public Task SaveWorkspaceAsync(string root, Manifest manifest, CancellationToken ct = default)
    {
        var ws = new WorkspaceMetadata
        {
            ProjectId = manifest.ProjectId,
            Name = manifest.Name,
            GameSlug = manifest.GameSlug,
            GameId = manifest.GameId,
            Tags = manifest.Tags,
            AttributeDefinitions = manifest.AttributeDefinitions,
        };
        return _backend.WriteTextAsync(WorkspacePath(root), Serialize(ws), ct);
    }

    private async Task MigrateLegacyAsync(string root, string legacyJson, CancellationToken ct)
    {
        var legacy = _manifests.Deserialize(legacyJson);
        await SaveWorkspaceAsync(root, legacy, ct).ConfigureAwait(false);
        foreach (var (path, file) in legacy.Files)
        {
            var shard = new AssetShard { Path = path, File = file };
            await _backend.WriteTextAsync(ShardPath(root, path), Serialize(shard), ct).ConfigureAwait(false);
        }
    }

    private async Task<AssetShard?> ReadShardCachedAsync(string remotePath, DateTimeOffset? mtime, CancellationToken ct)
    {
        if (mtime is not null && _cache.TryGetValue(remotePath, out var cached) && cached.MTime == mtime)
            return cached.Shard;

        var json = await _backend.ReadTextAsync(remotePath, ct).ConfigureAwait(false);
        if (json is null)
        {
            _cache.Remove(remotePath);
            return null;
        }
        var shard = Deserialize<AssetShard>(json);
        _cache[remotePath] = (mtime, shard);
        return shard;
    }

    /// <summary>Recursively list every shard file under <c>.gamestack/files</c> with its last-modified time.</summary>
    private async Task<List<(string RemotePath, DateTimeOffset? MTime)>> ListShardFilesAsync(string root, CancellationToken ct)
    {
        var result = new List<(string, DateTimeOffset?)>();
        var pending = new Stack<string>();
        pending.Push(FilesRoot(root));
        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var entry in await _backend.ListAsync(pending.Pop(), ct).ConfigureAwait(false))
            {
                if (entry.Kind == RemoteEntryKind.Folder)
                    pending.Push(entry.Path);
                else if (entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    result.Add((entry.Path, entry.ModifiedUtc));
            }
        }
        return result;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ManifestService.JsonOptions);

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, ManifestService.JsonOptions)
           ?? throw new InvalidDataException($"{typeof(T).Name} JSON deserialized to null.");

    private static string Join(string root, string relative)
    {
        root = root.Trim('/');
        relative = relative.Trim('/');
        return root.Length == 0 ? relative : $"{root}/{relative}";
    }
}
