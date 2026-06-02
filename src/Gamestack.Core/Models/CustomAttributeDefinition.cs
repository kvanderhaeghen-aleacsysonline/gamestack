namespace Gamestack.Core.Models;

/// <summary>The value type of a workspace-defined custom attribute.</summary>
public enum AttributeValueType
{
    /// <summary>Free-form text.</summary>
    Text,

    /// <summary>A numeric value (stored as its invariant string form).</summary>
    Number,

    /// <summary>A boolean flag (<c>true</c>/<c>false</c>).</summary>
    Boolean,

    /// <summary>A date (stored as ISO-8601 <c>yyyy-MM-dd</c>).</summary>
    Date,
}

/// <summary>
/// A workspace-defined custom metadata field that can be set per asset (e.g. "Artist" : Text,
/// "Approved" : Boolean). Definitions live in the <see cref="Manifest"/>; per-file values live in
/// <see cref="AssetFile.Attributes"/> keyed by <see cref="Key"/>.
/// </summary>
public sealed class CustomAttributeDefinition
{
    /// <summary>Unique attribute key (case-insensitive), used as the per-file value key.</summary>
    public required string Key { get; set; }

    /// <summary>The value type this attribute accepts.</summary>
    public AttributeValueType Type { get; set; } = AttributeValueType.Text;
}
