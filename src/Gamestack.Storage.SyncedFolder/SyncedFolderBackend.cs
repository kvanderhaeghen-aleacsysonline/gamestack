using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;

namespace Gamestack.Storage.SyncedFolder;

/// <summary>
/// <see cref="IStorageBackend"/> implementation that treats a local folder as the "remote".
/// In production that folder is the machine's OneDrive/SharePoint <b>sync-client root</b> (or a
/// sub-folder of it): writing a file there hands it to the OneDrive client, which uploads it to
/// the cloud asynchronously. No Microsoft Graph, MSAL, or Azure AD app registration is involved.
/// </summary>
/// <remarks>
/// Because the cloud transfer is owned by the OneDrive client, two operations are intentionally
/// limited: <see cref="ListVersionsAsync"/> reports only the current on-disk file (server-side
/// version history is not visible through the filesystem), and <see cref="DownloadVersionAsync"/>
/// is not supported. App-managed version metadata lives in the project manifest instead.
/// </remarks>
public sealed class SyncedFolderBackend : IStorageBackend
{
    private const int BufferSize = 1 << 20; // 1 MiB
    private readonly string _root;

    /// <summary>Create a backend rooted at the given synced-folder path.</summary>
    /// <param name="syncedRootPath">Absolute path to the OneDrive/SharePoint synced folder (or sub-folder).</param>
    public SyncedFolderBackend(string syncedRootPath)
        => _root = Path.GetFullPath(syncedRootPath);

    /// <summary>The absolute synced-folder root this backend reads and writes.</summary>
    public string Root => _root;

    /// <inheritdoc />
    public Task<IReadOnlyList<RemoteEntry>> ListAsync(string remotePath, CancellationToken ct = default)
    {
        var dir = Resolve(remotePath);
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<RemoteEntry>>(Array.Empty<RemoteEntry>());

        var entries = new List<RemoteEntry>();
        foreach (var info in new DirectoryInfo(dir).EnumerateFileSystemInfos())
        {
            ct.ThrowIfCancellationRequested();
            var isDir = (info.Attributes & FileAttributes.Directory) != 0;
            var rel = Combine(remotePath, info.Name);
            entries.Add(new RemoteEntry(
                rel,
                info.Name,
                isDir ? RemoteEntryKind.Folder : RemoteEntryKind.File,
                isDir ? 0 : ((FileInfo)info).Length,
                info.LastWriteTimeUtc,
                rel));
        }
        return Task.FromResult<IReadOnlyList<RemoteEntry>>(entries);
    }

    /// <inheritdoc />
    public async Task DownloadAsync(string remotePath, Stream destination, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        await using var fs = new FileStream(Resolve(remotePath), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        await CopyAsync(fs, destination, progress, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RemoteFileInfo> UploadAsync(string remotePath, Stream source, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var target = Resolve(remotePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        long size;
        await using (var fs = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
        {
            size = await CopyAsync(source, fs, progress, ct).ConfigureAwait(false);
        }

        // The OneDrive client versions the file server-side; we expose its write time as the pointer.
        var versionId = File.GetLastWriteTimeUtc(target).Ticks.ToString();
        return new RemoteFileInfo(remotePath, versionId, size);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RemoteVersion>> ListVersionsAsync(string remotePath, CancellationToken ct = default)
    {
        var path = Resolve(remotePath);
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<RemoteVersion>>(Array.Empty<RemoteVersion>());

        var fi = new FileInfo(path);
        var current = new RemoteVersion(fi.LastWriteTimeUtc.Ticks.ToString(), fi.Length, fi.LastWriteTimeUtc, null);
        return Task.FromResult<IReadOnlyList<RemoteVersion>>(new[] { current });
    }

    /// <inheritdoc />
    public Task DownloadVersionAsync(string remotePath, string versionId, Stream destination, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Fetching a specific historical version is not available through the OneDrive/SharePoint synced folder. " +
            "Version metadata is tracked in the project manifest; old bytes must be retrieved via the SharePoint web UI or a future Graph backend.");

    /// <inheritdoc />
    public async Task<string?> ReadTextAsync(string remotePath, CancellationToken ct = default)
    {
        var path = Resolve(remotePath);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteTextAsync(string remotePath, string content, CancellationToken ct = default)
    {
        var path = Resolve(remotePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
    }

    private string Resolve(string remotePath)
    {
        var rel = remotePath.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, rel));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Resolved path '{full}' escapes the synced-folder root.");
        return full;
    }

    private static string Combine(string remotePath, string name)
    {
        var prefix = remotePath.Trim('/');
        return prefix.Length == 0 ? name : $"{prefix}/{name}";
    }

    private static async Task<long> CopyAsync(Stream source, Stream destination, IProgress<long>? progress, CancellationToken ct)
    {
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            total += read;
            progress?.Report(total);
        }
        return total;
    }
}
