namespace Gamestack.Core.Models;

/// <summary>Manifest entry for one tracked asset: its app-managed versions and feedback thread.</summary>
public sealed class AssetFile
{
    /// <summary>Highest version number assigned so far (equals the latest entry in <see cref="Versions"/>).</summary>
    public int CurrentVersion { get; set; }

    /// <summary>All versions of the asset, oldest first.</summary>
    public List<AssetVersion> Versions { get; set; } = new();

    /// <summary>Feedback chat thread for this asset.</summary>
    public List<Comment> Comments { get; set; } = new();

    /// <summary>Tags assigned to this asset (a subset of the manifest's <see cref="Manifest.Tags"/> vocabulary).</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Custom attribute values for this asset, keyed by <see cref="CustomAttributeDefinition.Key"/>.
    /// Values are stored as strings interpreted per the matching definition's type.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}
