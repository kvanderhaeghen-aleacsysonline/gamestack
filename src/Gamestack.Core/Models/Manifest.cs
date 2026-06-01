namespace Gamestack.Core.Models;

/// <summary>
/// Per-project manifest, stored in the cloud at <c>&lt;project&gt;/.gamestack/manifest.json</c> so all
/// clients share it. It is the source of truth for app-managed versions, feedback threads, and the
/// game linkage that powers the future VS Code extension.
/// </summary>
public sealed class Manifest
{
    /// <summary>Stable unique id for the project.</summary>
    public required string ProjectId { get; set; }

    /// <summary>Human-readable project name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Links this project folder to a game slug (e.g. <c>cosmic-slots</c>).</summary>
    public string? GameSlug { get; set; }

    /// <summary>Links this project folder to a game id.</summary>
    public string? GameId { get; set; }

    /// <summary>Tracked files keyed by workspace-relative path ('/'-separated).</summary>
    public Dictionary<string, AssetFile> Files { get; set; } = new();
}
