using Gamestack.Core.Settings;

namespace Gamestack.Core.Abstractions;

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Load settings, returning defaults if none have been saved yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>Persist the given settings.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
