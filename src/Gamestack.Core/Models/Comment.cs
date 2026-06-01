namespace Gamestack.Core.Models;

/// <summary>Nature of a feedback message; <see cref="CommentKind.RequestChanges"/> and
/// <see cref="CommentKind.Approve"/> drive the (phase 2) review workflow.</summary>
public enum CommentKind
{
    /// <summary>Plain feedback with no review verdict.</summary>
    Comment,
    /// <summary>Reviewer requests changes before approval.</summary>
    RequestChanges,
    /// <summary>Reviewer approves the asset version.</summary>
    Approve,
}

/// <summary>A single message in an asset's feedback chat thread, stored in the project manifest.</summary>
public sealed class Comment
{
    /// <summary>Stable unique id of the message.</summary>
    public required string Id { get; set; }

    /// <summary>Author, taken from the connected account at post time.</summary>
    public required UserIdentity Author { get; set; }

    /// <summary>UTC timestamp the message was posted.</summary>
    public DateTimeOffset AtUtc { get; set; }

    /// <summary>Asset version the message refers to (0 means "general / not version-specific").</summary>
    public int Version { get; set; }

    /// <summary>Message body.</summary>
    public string Text { get; set; } = "";

    /// <summary>Whether this is plain feedback or a review verdict.</summary>
    public CommentKind Kind { get; set; } = CommentKind.Comment;
}
