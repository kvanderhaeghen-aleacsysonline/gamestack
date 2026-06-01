using Gamestack.Core.Models;

namespace Gamestack.Core.Abstractions;

/// <summary>
/// Abstraction over a cloud backend. OneDrive/SharePoint (Microsoft Graph) is the first
/// implementation; GitHub (+Git LFS) and Google Drive can be added later. The UI and the
/// sync engine depend only on this interface — never on a concrete provider.
/// All <c>remotePath</c> arguments are workspace-relative and '/'-separated.
/// </summary>
public interface IStorageBackend
{
    /// <summary>List the immediate children of a remote folder.</summary>
    Task<IReadOnlyList<RemoteEntry>> ListAsync(string remotePath, CancellationToken ct = default);

    /// <summary>Download the current content of a remote file into <paramref name="destination"/>.</summary>
    Task DownloadAsync(string remotePath, Stream destination, IProgress<long>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Upload <paramref name="source"/> to a remote file, creating a new backend version.
    /// Implementations must use chunked/resumable upload for large files.
    /// </summary>
    Task<RemoteFileInfo> UploadAsync(string remotePath, Stream source, IProgress<long>? progress = null, CancellationToken ct = default);

    /// <summary>List the backend-native versions of a remote file, newest first.</summary>
    Task<IReadOnlyList<RemoteVersion>> ListVersionsAsync(string remotePath, CancellationToken ct = default);

    /// <summary>Download a specific backend version of a remote file.</summary>
    Task DownloadVersionAsync(string remotePath, string versionId, Stream destination, CancellationToken ct = default);

    /// <summary>Read a small text file (e.g. a manifest); returns <c>null</c> if it does not exist.</summary>
    Task<string?> ReadTextAsync(string remotePath, CancellationToken ct = default);

    /// <summary>Create or overwrite a small text file (e.g. a manifest).</summary>
    Task WriteTextAsync(string remotePath, string content, CancellationToken ct = default);
}
