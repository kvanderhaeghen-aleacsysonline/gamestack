using System.Text.Json;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Versioning;

namespace Gamestack.Infrastructure;

/// <summary>
/// File-based <see cref="ILocalStateStore"/> (JSON document under <c>%AppData%\Gamestack\state.json</c>
/// by default). Dependency-free; the <see cref="ILocalStateStore"/> abstraction lets this be swapped
/// for a database implementation later without touching callers.
/// </summary>
public sealed class JsonLocalStateStore : ILocalStateStore
{
    private sealed class State
    {
        public Dictionary<string, SyncBaseline> Baselines { get; set; } = new(StringComparer.Ordinal);
        public HashSet<string> Materialized { get; set; } = new(StringComparer.Ordinal);
    }

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private State? _state;

    /// <summary>Create a store at the given file path, or the default app-data location.</summary>
    public JsonLocalStateStore(string? path = null)
        => _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gamestack", "state.json");

    public async Task<bool> IsMaterializedAsync(string path, CancellationToken ct = default)
    {
        var state = await EnsureLoadedAsync(ct).ConfigureAwait(false);
        lock (state) return state.Materialized.Contains(path);
    }

    public Task SetMaterializedAsync(string path, bool materialized, CancellationToken ct = default)
        => MutateAsync(state =>
        {
            if (materialized) state.Materialized.Add(path); else state.Materialized.Remove(path);
        }, ct);

    public async Task<SyncBaseline?> GetBaselineAsync(string path, CancellationToken ct = default)
    {
        var state = await EnsureLoadedAsync(ct).ConfigureAwait(false);
        lock (state) return state.Baselines.TryGetValue(path, out var b) ? b : null;
    }

    public Task SetBaselineAsync(SyncBaseline baseline, CancellationToken ct = default)
        => MutateAsync(state => state.Baselines[baseline.Path] = baseline, ct);

    public Task RemoveAsync(string path, CancellationToken ct = default)
        => MutateAsync(state =>
        {
            state.Baselines.Remove(path);
            state.Materialized.Remove(path);
        }, ct);

    public async Task<IReadOnlyList<SyncBaseline>> GetAllBaselinesAsync(CancellationToken ct = default)
    {
        var state = await EnsureLoadedAsync(ct).ConfigureAwait(false);
        lock (state) return state.Baselines.Values.ToList();
    }

    private async Task<State> EnsureLoadedAsync(CancellationToken ct)
    {
        if (_state is not null)
            return _state;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_state is null)
            {
                if (File.Exists(_path))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(_path, ct).ConfigureAwait(false);
                        _state = JsonSerializer.Deserialize<State>(json, ManifestService.JsonOptions) ?? new State();
                    }
                    catch (JsonException) { _state = new State(); }
                }
                else
                {
                    _state = new State();
                }
            }
        }
        finally { _gate.Release(); }
        return _state;
    }

    private async Task MutateAsync(Action<State> mutation, CancellationToken ct)
    {
        var state = await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            lock (state) mutation(state);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string json;
            lock (state) json = JsonSerializer.Serialize(state, ManifestService.JsonOptions);
            await File.WriteAllTextAsync(_path, json, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
