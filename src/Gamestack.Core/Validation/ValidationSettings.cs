namespace Gamestack.Core.Validation;

/// <summary>How a required Spine version is matched against an exported skeleton's version.</summary>
public enum SpineVersionMatch
{
    /// <summary>The full version must match exactly (e.g. <c>4.1.23</c>).</summary>
    Exact,
    /// <summary>Only major.minor must match (e.g. <c>4.1</c>).</summary>
    MajorMinor,
}

/// <summary>
/// User-configurable validation options (persisted in app settings, shown under the
/// "Validation" section). Each rule is independent; rules that fail produce a warning by
/// default, or a blocking error when the matching "block push" flag is enabled.
/// </summary>
public sealed class ValidationSettings
{
    /// <summary>Master switch for all validation.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Require image width and height to each be divisible by 4.</summary>
    public bool RequireDivisibleByFour { get; set; }

    /// <summary>Require image width to equal its height.</summary>
    public bool RequireSquare { get; set; }

    /// <summary>Require image width and height to each be a power of two.</summary>
    public bool RequirePowerOfTwo { get; set; }

    /// <summary>Treat image-rule failures as push-blocking errors instead of warnings.</summary>
    public bool BlockOnImageFailure { get; set; }

    /// <summary>Enable the Spine exported-version check.</summary>
    public bool CheckSpineVersion { get; set; }

    /// <summary>The required Spine version (e.g. <c>4.1.23</c> or <c>4.1</c>).</summary>
    public string? RequiredSpineVersion { get; set; }

    /// <summary>How <see cref="RequiredSpineVersion"/> is matched.</summary>
    public SpineVersionMatch SpineMatch { get; set; } = SpineVersionMatch.MajorMinor;

    /// <summary>Treat Spine-version failures as push-blocking errors instead of warnings.</summary>
    public bool BlockOnSpineFailure { get; set; }
}
