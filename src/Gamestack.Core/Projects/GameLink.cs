namespace Gamestack.Core.Projects;

/// <summary>
/// The contents of a per-directory <c>gamestack.json</c> marker, linking a top-level project folder
/// to a game. This is the format a future VS Code extension will read to discover a game's assets.
/// </summary>
public sealed class GameLink
{
    /// <summary>The game identifier this directory's assets belong to. Defaults to the folder name.</summary>
    public required string GameId { get; set; }
}

/// <summary>A top-level project directory in the synced folder, with its game link (if marked).</summary>
/// <param name="Path">Path relative to the synced-folder root ('/'-separated).</param>
/// <param name="Name">Folder name.</param>
/// <param name="Link">The parsed <c>gamestack.json</c>, or <c>null</c> if the directory has none yet.</param>
public sealed record ProjectDirectory(string Path, string Name, GameLink? Link);
