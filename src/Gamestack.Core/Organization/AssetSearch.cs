using Gamestack.Core.Models;

namespace Gamestack.Core.Organization;

/// <summary>
/// Criteria for searching the assets tracked in a <see cref="Manifest"/>. All set criteria must
/// match (logical AND); unset criteria are ignored.
/// </summary>
public sealed class SearchQuery
{
    /// <summary>Case-insensitive substring matched against the file's workspace-relative path.</summary>
    public string? Text { get; set; }

    /// <summary>Tags that must all be present on the asset (case-insensitive).</summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>File extension to match, with or without a leading dot (e.g. <c>png</c> or <c>.png</c>).</summary>
    public string? Extension { get; set; }

    /// <summary>Game id to match against the manifest's <see cref="Manifest.GameId"/>.</summary>
    public string? GameId { get; set; }

    /// <summary>Custom attribute key/value pairs that must all match (case-insensitive value compare).</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; set; } =
        new Dictionary<string, string>();

    /// <summary>True when no criteria are set (matches everything).</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Text) && Tags.Count == 0 &&
        string.IsNullOrWhiteSpace(Extension) && string.IsNullOrWhiteSpace(GameId) &&
        Attributes.Count == 0;
}

/// <summary>One asset matched by a <see cref="SearchQuery"/>.</summary>
public sealed class AssetSearchResult
{
    /// <summary>Workspace-relative path of the matched asset.</summary>
    public required string Path { get; init; }

    /// <summary>The matched asset's manifest entry.</summary>
    public required AssetFile File { get; init; }
}

/// <summary>Pure search over the assets tracked in a <see cref="Manifest"/>.</summary>
public static class AssetSearch
{
    /// <summary>
    /// Return all assets in <paramref name="manifest"/> matching <paramref name="query"/>, ordered
    /// by path. An empty query returns every tracked file.
    /// </summary>
    public static IReadOnlyList<AssetSearchResult> Search(Manifest manifest, SearchQuery query)
    {
        var ext = NormalizeExtension(query.Extension);
        var gameMismatch =
            !string.IsNullOrWhiteSpace(query.GameId) &&
            !string.Equals(manifest.GameId, query.GameId, StringComparison.OrdinalIgnoreCase);
        if (gameMismatch)
            return Array.Empty<AssetSearchResult>();

        var results = new List<AssetSearchResult>();
        foreach (var (path, file) in manifest.Files)
        {
            if (!string.IsNullOrWhiteSpace(query.Text) &&
                path.IndexOf(query.Text, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (ext is not null &&
                !path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                continue;

            if (query.Tags.Count > 0 &&
                !query.Tags.All(t => file.Tags.Any(ft => string.Equals(ft, t, StringComparison.OrdinalIgnoreCase))))
                continue;

            if (query.Attributes.Count > 0 && !MatchesAttributes(file, query.Attributes))
                continue;

            results.Add(new AssetSearchResult { Path = path, File = file });
        }

        results.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static bool MatchesAttributes(AssetFile file, IReadOnlyDictionary<string, string> wanted)
    {
        foreach (var (key, value) in wanted)
        {
            if (!file.Attributes.TryGetValue(key, out var actual) ||
                !string.Equals(actual, value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static string? NormalizeExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return null;
        ext = ext.Trim();
        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
