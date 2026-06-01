namespace Gamestack.Core.Models;

/// <summary>State of a review on an asset version.</summary>
public enum ReviewStatus
{
    /// <summary>A reviewer has been assigned and has not yet responded.</summary>
    Pending,
    /// <summary>The reviewer approved the version.</summary>
    Approved,
    /// <summary>The reviewer asked for changes.</summary>
    ChangesRequested,
}

/// <summary>
/// A request for a specific person to review a specific asset version. The back-and-forth is
/// carried by the asset's <see cref="Comment"/> thread; this record tracks the assignment and the
/// latest verdict so an inbox can list outstanding work.
/// </summary>
public sealed class ReviewRequest
{
    /// <summary>The assigned reviewer (email is used for notifications and inbox matching).</summary>
    public required UserIdentity Reviewer { get; set; }

    /// <summary>Who requested the review.</summary>
    public required UserIdentity RequestedBy { get; set; }

    /// <summary>When the review was requested (UTC).</summary>
    public DateTimeOffset RequestedAtUtc { get; set; }

    /// <summary>Current status.</summary>
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    /// <summary>When the reviewer responded (UTC), if they have.</summary>
    public DateTimeOffset? DecidedAtUtc { get; set; }
}
