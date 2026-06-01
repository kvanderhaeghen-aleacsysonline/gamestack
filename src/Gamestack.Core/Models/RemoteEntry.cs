namespace Gamestack.Core.Models;

/// <summary>Whether a remote entry is a file or a folder.</summary>
public enum RemoteEntryKind
{
    /// <summary>A file.</summary>
    File,
    /// <summary>A folder/directory.</summary>
    Folder,
}

/// <summary>A single entry returned when listing a remote folder via <see cref="Abstractions.IStorageBackend"/>.</summary>
/// <param name="Path">Workspace-relative path, '/'-separated.</param>
/// <param name="Name">Leaf name.</param>
/// <param name="Kind">File or folder.</param>
/// <param name="Size">Size in bytes (0 for folders).</param>
/// <param name="ModifiedUtc">Last-modified time, when known.</param>
/// <param name="BackendId">Opaque backend identifier (e.g. OneDrive DriveItem id).</param>
public sealed record RemoteEntry(
    string Path,
    string Name,
    RemoteEntryKind Kind,
    long Size,
    DateTimeOffset? ModifiedUtc,
    string? BackendId);

/// <summary>Result of uploading a file: pointers to the freshly created backend version.</summary>
/// <param name="BackendId">Backend identifier of the file (e.g. DriveItem id).</param>
/// <param name="BackendVersionId">Backend-native version id holding these bytes.</param>
/// <param name="Size">Uploaded size in bytes.</param>
public sealed record RemoteFileInfo(string BackendId, string BackendVersionId, long Size);

/// <summary>A backend-native version of a file (e.g. an OneDrive <c>DriveItemVersion</c>).</summary>
/// <param name="VersionId">Backend version identifier.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="ModifiedUtc">When this version was created.</param>
/// <param name="ModifiedBy">Who created it, when known.</param>
public sealed record RemoteVersion(string VersionId, long Size, DateTimeOffset ModifiedUtc, string? ModifiedBy);
