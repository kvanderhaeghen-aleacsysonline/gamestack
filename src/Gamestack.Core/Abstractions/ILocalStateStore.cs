namespace Gamestack.Core.Abstractions;

/// <summary>
/// The last-synced baseline for one file. Local change detection compares the current file's
/// hash against <see cref="Sha256"/> to decide whether it is "ready to push".
/// </summary>
/// <param name="Path">Workspace-relative path ('/'-separated).</param>
/// <param name="Version">Version number that was synced.</param>
/// <param name="Sha256">Content hash (hex) at sync time.</param>
/// <param name="Size">Size in bytes at sync time.</param>
/// <param name="MTimeUtc">Last-write time at sync time (fast-path change hint).</param>
public sealed record SyncBaseline(string Path, int Version, string Sha256, long Size, DateTimeOffset MTimeUtc);

/// <summary>
/// Local persisted client state (selective-sync membership and per-file synced baselines).
/// Backed by LiteDB in the app; an in-memory implementation is used in tests.
/// </summary>
public interface ILocalStateStore
{
    /// <summary>Whether the given path is materialized locally (selected for sync).</summary>
    Task<bool> IsMaterializedAsync(string path, CancellationToken ct = default);

    /// <summary>Mark a path as materialized or not.</summary>
    Task SetMaterializedAsync(string path, bool materialized, CancellationToken ct = default);

    /// <summary>Get the synced baseline for a path, or <c>null</c> if none.</summary>
    Task<SyncBaseline?> GetBaselineAsync(string path, CancellationToken ct = default);

    /// <summary>Create or update the synced baseline for a path.</summary>
    Task SetBaselineAsync(SyncBaseline baseline, CancellationToken ct = default);

    /// <summary>Remove all stored state for a path.</summary>
    Task RemoveAsync(string path, CancellationToken ct = default);

    /// <summary>Get every stored baseline (used for full workspace scans).</summary>
    Task<IReadOnlyList<SyncBaseline>> GetAllBaselinesAsync(CancellationToken ct = default);
}
