using System.Text.Json;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Settings;
using Gamestack.Core.Versioning;

namespace Gamestack.Infrastructure;

/// <summary>
/// <see cref="ISettingsStore"/> that persists <see cref="AppSettings"/> as a JSON file
/// (by default under <c>%AppData%\Gamestack\settings.json</c>).
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Create a store at the given file path, or the default app-data location.</summary>
    public JsonSettingsStore(string? path = null)
        => _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gamestack", "settings.json");

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return new AppSettings();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json = await File.ReadAllTextAsync(_path, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppSettings>(json, ManifestService.JsonOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings(); // corrupt file — start fresh rather than crash
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(settings, ManifestService.JsonOptions);
            await File.WriteAllTextAsync(_path, json, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
