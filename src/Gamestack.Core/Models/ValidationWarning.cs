namespace Gamestack.Core.Models;

/// <summary>Severity of a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Non-blocking advisory.</summary>
    Warning,
    /// <summary>Blocking failure (when the relevant "block push" setting is enabled).</summary>
    Error,
}

/// <summary>
/// A single validation finding for an asset (e.g. non-square image, wrong Spine version),
/// surfaced as a badge next to the file in the Changes/Explorer views.
/// </summary>
/// <param name="RuleId">Stable rule identifier (e.g. <c>image.square</c>, <c>spine.version</c>).</param>
/// <param name="Severity">Whether the finding blocks a push.</param>
/// <param name="Message">Human-readable explanation shown in the tooltip.</param>
public sealed record ValidationWarning(string RuleId, ValidationSeverity Severity, string Message);
