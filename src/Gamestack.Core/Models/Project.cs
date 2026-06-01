namespace Gamestack.Core.Models;

/// <summary>
/// A project in the local workspace: the mapping between a local folder, its remote root, and the
/// game it belongs to. Runtime view; durable version/feedback data lives in the <see cref="Manifest"/>.
/// </summary>
public sealed class Project
{
    /// <summary>Stable unique id (matches <see cref="Manifest.ProjectId"/>).</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable project name.</summary>
    public required string Name { get; set; }

    /// <summary>Absolute local folder path where the project's files are materialized.</summary>
    public required string LocalPath { get; set; }

    /// <summary>Remote root path of the project within the workspace ('/'-separated).</summary>
    public required string RemotePath { get; set; }

    /// <summary>Linked game slug, if any.</summary>
    public string? GameSlug { get; set; }

    /// <summary>Linked game id, if any.</summary>
    public string? GameId { get; set; }
}
