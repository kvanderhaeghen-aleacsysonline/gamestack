namespace Gamestack.Core.Models;

/// <summary>
/// Workspace-level metadata stored once per workspace at <c>.gamestack/workspace.json</c>: project
/// identity, game linkage, and the shared tag vocabulary + custom-attribute definitions. Per-asset
/// data lives in individual shard files (see <see cref="AssetShard"/>) rather than here, so this
/// file stays small and is written only when workspace-wide settings change.
/// </summary>
public sealed class WorkspaceMetadata
{
    /// <summary>Stable unique id for the project/workspace.</summary>
    public required string ProjectId { get; set; }

    /// <summary>Human-readable project name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Links this workspace to a game slug (e.g. <c>cosmic-slots</c>).</summary>
    public string? GameSlug { get; set; }

    /// <summary>Links this workspace to a game id.</summary>
    public string? GameId { get; set; }

    /// <summary>Workspace-wide tag vocabulary; per-asset tags are drawn from this list.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Workspace-defined custom attribute fields available to assign per asset.</summary>
    public List<CustomAttributeDefinition> AttributeDefinitions { get; set; } = new();
}

/// <summary>
/// On-disk shard for a single asset, stored at <c>.gamestack/files/&lt;path&gt;.json</c>. Carries its
/// own <see cref="Path"/> so the asset can be identified without decoding the file name.
/// </summary>
public sealed class AssetShard
{
    /// <summary>Workspace-relative path of the asset ('/'-separated).</summary>
    public required string Path { get; set; }

    /// <summary>The asset's versions, comments, tags, and attributes.</summary>
    public required AssetFile File { get; set; }
}
