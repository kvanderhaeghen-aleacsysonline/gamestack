using System.Text.Json;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Sync;
using Gamestack.Core.Versioning;

namespace Gamestack.Core.Projects;

/// <summary>
/// Manages the per-directory <c>gamestack.json</c> markers that link each top-level project folder
/// to a game. Works through <see cref="IStorageBackend"/>, so it applies equally to the synced
/// folder or any future backend.
/// </summary>
public sealed class GameLinkService
{
    /// <summary>The marker file name written into each top-level project directory.</summary>
    public const string MarkerFileName = "gamestack.json";

    /// <summary>Read a directory's <c>gamestack.json</c>, or <c>null</c> if it has none / is unreadable.</summary>
    public async Task<GameLink?> ReadAsync(IStorageBackend backend, string directoryPath, CancellationToken ct = default)
    {
        var json = await backend.ReadTextAsync(MarkerPath(directoryPath), ct).ConfigureAwait(false);
        if (json is null) return null;
        try { return JsonSerializer.Deserialize<GameLink>(json, ManifestService.JsonOptions); }
        catch (JsonException) { return null; }
    }

    /// <summary>Write (create or overwrite) a directory's <c>gamestack.json</c>.</summary>
    public Task WriteAsync(IStorageBackend backend, string directoryPath, GameLink link, CancellationToken ct = default)
        => backend.WriteTextAsync(MarkerPath(directoryPath), JsonSerializer.Serialize(link, ManifestService.JsonOptions), ct);

    /// <summary>List the top-level project directories and each one's game link (if present).</summary>
    public async Task<IReadOnlyList<ProjectDirectory>> ScanAsync(IStorageBackend backend, CancellationToken ct = default)
    {
        var result = new List<ProjectDirectory>();
        foreach (var entry in await backend.ListAsync("", ct).ConfigureAwait(false))
        {
            if (entry.Kind != RemoteEntryKind.Folder)
                continue;
            if (entry.Name.Equals(ChangeDetector.MetadataFolder, StringComparison.OrdinalIgnoreCase))
                continue;

            var link = await ReadAsync(backend, entry.Path, ct).ConfigureAwait(false);
            result.Add(new ProjectDirectory(entry.Path, entry.Name, link));
        }
        return result;
    }

    /// <summary>Count top-level directories that don't yet have a <c>gamestack.json</c>.</summary>
    public async Task<int> CountMissingAsync(IStorageBackend backend, CancellationToken ct = default)
        => (await ScanAsync(backend, ct).ConfigureAwait(false)).Count(p => p.Link is null);

    /// <summary>
    /// Create a default marker (<c>gameId</c> = folder name) in every top-level directory that lacks
    /// one. Existing markers are left untouched. Returns the number created.
    /// </summary>
    public async Task<int> EnsureMarkersAsync(IStorageBackend backend, CancellationToken ct = default)
    {
        var created = 0;
        foreach (var dir in await ScanAsync(backend, ct).ConfigureAwait(false))
        {
            if (dir.Link is not null)
                continue;
            await WriteAsync(backend, dir.Path, new GameLink { GameId = dir.Name }, ct).ConfigureAwait(false);
            created++;
        }
        return created;
    }

    /// <summary>
    /// Create a brand-new game: makes a top-level folder named <paramref name="name"/> in the synced
    /// root (by writing its <c>gamestack.json</c>, which creates the directory) with <c>gameId</c> set
    /// to the same name. Throws if the name is invalid or a folder by that name already exists.
    /// </summary>
    public async Task CreateGameAsync(IStorageBackend backend, string name, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("A game name is required.", nameof(name));
        if (name.Contains('/') || name.Contains('\\') || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("The game name contains invalid characters.", nameof(name));

        var existing = await ScanAsync(backend, ct).ConfigureAwait(false);
        if (existing.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A folder named '{name}' already exists.");

        await WriteAsync(backend, name, new GameLink { GameId = name }, ct).ConfigureAwait(false);
    }

    private static string MarkerPath(string directoryPath)
        => $"{directoryPath.Trim('/')}/{MarkerFileName}";
}
