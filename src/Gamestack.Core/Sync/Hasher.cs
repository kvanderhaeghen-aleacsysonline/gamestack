using System.Security.Cryptography;

namespace Gamestack.Core.Sync;

/// <summary>Computes content hashes used to detect local changes and to stamp versions.</summary>
public static class Hasher
{
    /// <summary>Compute the lowercase-hex SHA-256 of a stream (read from its current position).</summary>
    public static async Task<string> Sha256Async(Stream stream, CancellationToken ct = default)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Compute the lowercase-hex SHA-256 of a file's contents.</summary>
    public static async Task<string> Sha256FileAsync(string path, CancellationToken ct = default)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true);
        return await Sha256Async(fs, ct).ConfigureAwait(false);
    }
}
