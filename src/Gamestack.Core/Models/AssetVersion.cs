namespace Gamestack.Core.Models;

/// <summary>
/// One app-managed version of an asset, recorded in the project <see cref="Manifest"/>.
/// App-managed version numbers are consistent across backends; the bytes themselves are
/// referenced via <see cref="BackendVersionId"/> (e.g. an OneDrive DriveItemVersion).
/// </summary>
public sealed class AssetVersion
{
    /// <summary>App-assigned sequential version number (1-based).</summary>
    public int Version { get; set; }

    /// <summary>Backend-native version id holding the bytes for this version, when applicable.</summary>
    public string? BackendVersionId { get; set; }

    /// <summary>SHA-256 (hex) of the file content at this version.</summary>
    public string Sha256 { get; set; } = "";

    /// <summary>Size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Who pushed this version, taken from the connected account.</summary>
    public required UserIdentity PushedBy { get; set; }

    /// <summary>UTC timestamp of the push.</summary>
    public DateTimeOffset PushedAtUtc { get; set; }

    /// <summary>User-supplied description of what changed in this version.</summary>
    public string Description { get; set; } = "";

    /// <summary>The review assigned to this version, if any.</summary>
    public ReviewRequest? Review { get; set; }
}
