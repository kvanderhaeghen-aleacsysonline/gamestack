namespace Gamestack.Core.Organization;

/// <summary>
/// Splits file names into lowercase word tokens and matches them against a tag vocabulary, used to
/// auto-tag assets on add (e.g. <c>Red_sword-3.png</c> → <c>red</c>, <c>sword</c>, <c>3</c>, <c>png</c>).
/// </summary>
public static class FileNameTokenizer
{
    /// <summary>
    /// Tokenize a file name (or path) into distinct lowercase tokens, splitting on any character
    /// that is not a letter or digit (so <c>. - _</c>, spaces, slashes, etc. are separators).
    /// </summary>
    public static IReadOnlyList<string> Tokenize(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Array.Empty<string>();

        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var start = -1;
        for (var i = 0; i <= fileName.Length; i++)
        {
            var isWord = i < fileName.Length && char.IsLetterOrDigit(fileName[i]);
            if (isWord && start < 0)
            {
                start = i;
            }
            else if (!isWord && start >= 0)
            {
                var token = fileName[start..i].ToLowerInvariant();
                if (seen.Add(token))
                    tokens.Add(token);
                start = -1;
            }
        }
        return tokens;
    }

    /// <summary>
    /// Return the subset of <paramref name="vocabulary"/> tags whose name matches a token in
    /// <paramref name="fileName"/> (case-insensitive). Returned values preserve the vocabulary's
    /// original casing.
    /// </summary>
    public static IReadOnlyList<string> MatchTags(string fileName, IEnumerable<string> vocabulary)
    {
        var tokens = new HashSet<string>(Tokenize(fileName), StringComparer.Ordinal);
        if (tokens.Count == 0)
            return Array.Empty<string>();

        var matched = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in vocabulary)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            if (tokens.Contains(tag.ToLowerInvariant()) && seen.Add(tag))
                matched.Add(tag);
        }
        return matched;
    }
}
