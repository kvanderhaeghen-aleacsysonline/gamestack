using System.Text.Json;
using System.Text.Json.Serialization;
using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;

namespace Gamestack.Core.Versioning;

/// <summary>
/// Reads/writes the project <see cref="Manifest"/> and applies app-managed version and feedback
/// mutations. Pure with respect to I/O — callers persist the serialized result via the backend.
/// </summary>
public sealed class ManifestService
{
    private readonly IClock _clock;

    /// <summary>Shared JSON options: indented, camelCase, enums as strings.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Create the service with a clock used to stamp push/comment timestamps.</summary>
    public ManifestService(IClock clock) => _clock = clock;

    /// <summary>Serialize a manifest to JSON.</summary>
    public string Serialize(Manifest manifest) => JsonSerializer.Serialize(manifest, JsonOptions);

    /// <summary>Parse a manifest from JSON.</summary>
    public Manifest Deserialize(string json)
        => JsonSerializer.Deserialize<Manifest>(json, JsonOptions)
           ?? throw new InvalidDataException("Manifest JSON deserialized to null.");

    /// <summary>Create a fresh, empty manifest for a new project.</summary>
    public Manifest CreateNew(string name, string? gameSlug = null, string? gameId = null) => new()
    {
        ProjectId = Guid.NewGuid().ToString("n"),
        Name = name,
        GameSlug = gameSlug,
        GameId = gameId,
    };

    /// <summary>
    /// Append a new version for <paramref name="path"/> (creating the file entry if needed),
    /// incrementing the file's <see cref="AssetFile.CurrentVersion"/>. Returns the new version.
    /// </summary>
    public AssetVersion AddVersion(Manifest manifest, string path, string sha256, long size,
        UserIdentity pushedBy, string description, string? backendVersionId = null)
    {
        var file = GetOrAdd(manifest, path);
        var version = new AssetVersion
        {
            Version = file.CurrentVersion + 1,
            Sha256 = sha256,
            Size = size,
            PushedBy = pushedBy,
            PushedAtUtc = _clock.UtcNow,
            Description = description,
            BackendVersionId = backendVersionId,
        };
        file.Versions.Add(version);
        file.CurrentVersion = version.Version;
        return version;
    }

    /// <summary>Append a feedback message to an asset's thread. Returns the created comment.</summary>
    public Comment AddComment(Manifest manifest, string path, UserIdentity author, string text,
        int version = 0, CommentKind kind = CommentKind.Comment)
    {
        var file = GetOrAdd(manifest, path);
        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString("n"),
            Author = author,
            AtUtc = _clock.UtcNow,
            Version = version,
            Text = text,
            Kind = kind,
        };
        file.Comments.Add(comment);
        return comment;
    }

    /// <summary>Assign a reviewer to a specific version (status becomes Pending). Returns the request.</summary>
    public ReviewRequest RequestReview(Manifest manifest, string path, int version, UserIdentity reviewer, UserIdentity requestedBy)
    {
        var target = FindVersion(manifest, path, version);
        var request = new ReviewRequest
        {
            Reviewer = reviewer,
            RequestedBy = requestedBy,
            RequestedAtUtc = _clock.UtcNow,
            Status = ReviewStatus.Pending,
        };
        target.Review = request;
        return request;
    }

    /// <summary>
    /// Record a reviewer's verdict (<see cref="ReviewStatus.Approved"/> or
    /// <see cref="ReviewStatus.ChangesRequested"/>): updates the version's review status and appends
    /// a matching comment to the asset thread. Returns the created comment.
    /// </summary>
    public Comment SubmitReviewDecision(Manifest manifest, string path, int version, UserIdentity decidedBy, ReviewStatus decision, string commentText)
    {
        if (decision is not (ReviewStatus.Approved or ReviewStatus.ChangesRequested))
            throw new ArgumentException("Decision must be Approved or ChangesRequested.", nameof(decision));

        var target = FindVersion(manifest, path, version);
        if (target.Review is { } review)
        {
            review.Status = decision;
            review.DecidedAtUtc = _clock.UtcNow;
        }

        var kind = decision == ReviewStatus.Approved ? CommentKind.Approve : CommentKind.RequestChanges;
        return AddComment(manifest, path, decidedBy, commentText, version, kind);
    }

    private static AssetVersion FindVersion(Manifest manifest, string path, int version)
    {
        if (!manifest.Files.TryGetValue(path, out var file))
            throw new ArgumentException($"No tracked file at '{path}'.", nameof(path));
        return file.Versions.FirstOrDefault(v => v.Version == version)
            ?? throw new ArgumentException($"No version {version} for '{path}'.", nameof(version));
    }

    private static AssetFile GetOrAdd(Manifest manifest, string path)
    {
        if (!manifest.Files.TryGetValue(path, out var file))
        {
            file = new AssetFile();
            manifest.Files[path] = file;
        }
        return file;
    }
}
