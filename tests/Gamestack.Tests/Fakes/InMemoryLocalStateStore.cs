using Gamestack.Core.Abstractions;

namespace Gamestack.Tests.Fakes;

/// <summary>In-memory <see cref="ILocalStateStore"/> for tests.</summary>
public sealed class InMemoryLocalStateStore : ILocalStateStore
{
    private readonly Dictionary<string, SyncBaseline> _baselines = new(StringComparer.Ordinal);
    private readonly HashSet<string> _materialized = new(StringComparer.Ordinal);

    public Task<bool> IsMaterializedAsync(string path, CancellationToken ct = default)
        => Task.FromResult(_materialized.Contains(path));

    public Task SetMaterializedAsync(string path, bool materialized, CancellationToken ct = default)
    {
        if (materialized) _materialized.Add(path); else _materialized.Remove(path);
        return Task.CompletedTask;
    }

    public Task<SyncBaseline?> GetBaselineAsync(string path, CancellationToken ct = default)
        => Task.FromResult(_baselines.TryGetValue(path, out var b) ? b : null);

    public Task SetBaselineAsync(SyncBaseline baseline, CancellationToken ct = default)
    {
        _baselines[baseline.Path] = baseline;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string path, CancellationToken ct = default)
    {
        _baselines.Remove(path);
        _materialized.Remove(path);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SyncBaseline>> GetAllBaselinesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SyncBaseline>>(_baselines.Values.ToList());
}
