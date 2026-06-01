using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;

namespace Gamestack.Tests.Fakes;

/// <summary>In-memory <see cref="IStorageBackend"/> for tests, keeping per-path version history.</summary>
public sealed class InMemoryStorageBackend : IStorageBackend
{
    private readonly Dictionary<string, List<byte[]>> _files = new(StringComparer.Ordinal);

    /// <summary>Number of upload calls received (handy for assertions).</summary>
    public int UploadCount { get; private set; }

    public Task<IReadOnlyList<RemoteEntry>> ListAsync(string remotePath, CancellationToken ct = default)
    {
        var prefix = remotePath.Trim('/');
        prefix = prefix.Length == 0 ? "" : prefix + "/";
        var entries = _files.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => new RemoteEntry(k, k.Split('/').Last(), RemoteEntryKind.File, _files[k][^1].Length, null, k))
            .ToList();
        return Task.FromResult<IReadOnlyList<RemoteEntry>>(entries);
    }

    public async Task DownloadAsync(string remotePath, Stream destination, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var bytes = _files[remotePath][^1];
        await destination.WriteAsync(bytes, ct);
        progress?.Report(bytes.Length);
    }

    public async Task<RemoteFileInfo> UploadAsync(string remotePath, Stream source, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (!_files.TryGetValue(remotePath, out var versions))
            _files[remotePath] = versions = new List<byte[]>();
        versions.Add(bytes);
        UploadCount++;
        progress?.Report(bytes.Length);
        return new RemoteFileInfo(remotePath, $"v{versions.Count}", bytes.Length);
    }

    public Task<IReadOnlyList<RemoteVersion>> ListVersionsAsync(string remotePath, CancellationToken ct = default)
    {
        var versions = _files.TryGetValue(remotePath, out var list)
            ? list.Select((b, i) => new RemoteVersion($"v{i + 1}", b.Length, DateTimeOffset.UnixEpoch, null)).Reverse().ToList()
            : new List<RemoteVersion>();
        return Task.FromResult<IReadOnlyList<RemoteVersion>>(versions);
    }

    public async Task DownloadVersionAsync(string remotePath, string versionId, Stream destination, CancellationToken ct = default)
    {
        var index = int.Parse(versionId.TrimStart('v')) - 1;
        await destination.WriteAsync(_files[remotePath][index], ct);
    }

    public Task<string?> ReadTextAsync(string remotePath, CancellationToken ct = default)
        => Task.FromResult(_files.TryGetValue(remotePath, out var v)
            ? System.Text.Encoding.UTF8.GetString(v[^1])
            : null);

    public Task WriteTextAsync(string remotePath, string content, CancellationToken ct = default)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        if (!_files.TryGetValue(remotePath, out var versions))
            _files[remotePath] = versions = new List<byte[]>();
        versions.Add(bytes);
        return Task.CompletedTask;
    }
}
