namespace Gamestack.Core.Models;

/// <summary>How a local file differs from its last-synced baseline.</summary>
public enum ChangeKind
{
    /// <summary>New file not present in the baseline.</summary>
    Added,
    /// <summary>Existing file whose content changed.</summary>
    Modified,
    /// <summary>File removed locally that exists in the baseline.</summary>
    Deleted,
}

/// <summary>
/// A pending local change awaiting push, together with any validation findings so the
/// Changes view can show a warning badge and tooltip.
/// </summary>
public sealed class FileChange
{
    /// <summary>Workspace-relative path ('/'-separated).</summary>
    public required string Path { get; set; }

    /// <summary>Kind of change.</summary>
    public ChangeKind Kind { get; set; }

    /// <summary>Baseline size in bytes (0 for <see cref="ChangeKind.Added"/>).</summary>
    public long OldSize { get; set; }

    /// <summary>Current local size in bytes (0 for <see cref="ChangeKind.Deleted"/>).</summary>
    public long NewSize { get; set; }

    /// <summary>Version this change is based on (0 for a new file).</summary>
    public int BaselineVersion { get; set; }

    /// <summary>Validation findings for the current local content.</summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; set; } = Array.Empty<ValidationWarning>();
}
