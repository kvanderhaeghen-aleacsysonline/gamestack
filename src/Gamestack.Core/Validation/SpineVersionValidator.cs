using System.Text.Json;
using Gamestack.Core.Models;

namespace Gamestack.Core.Validation;

/// <summary>
/// Validates that an exported Spine skeleton JSON declares the required Spine editor version
/// (<c>"skeleton": { "spine": "x.y.z" }</c>). Non-Spine <c>.json</c> files (no such field) are
/// ignored rather than failed.
/// </summary>
public sealed class SpineVersionValidator : IAssetValidator
{
    /// <inheritdoc />
    public bool AppliesTo(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationWarning>> ValidateAsync(
        string localPath, ValidationSettings settings, CancellationToken ct = default)
    {
        if (!settings.Enabled || !settings.CheckSpineVersion || string.IsNullOrWhiteSpace(settings.RequiredSpineVersion))
            return Array.Empty<ValidationWarning>();

        var actual = await TryReadSpineVersionAsync(localPath, ct).ConfigureAwait(false);
        if (actual is null)
            return Array.Empty<ValidationWarning>(); // not a Spine skeleton export

        if (Matches(actual, settings.RequiredSpineVersion!, settings.SpineMatch))
            return Array.Empty<ValidationWarning>();

        var severity = settings.BlockOnSpineFailure ? ValidationSeverity.Error : ValidationSeverity.Warning;
        return new[]
        {
            new ValidationWarning("spine.version", severity,
                $"Spine version {actual} does not match required {settings.RequiredSpineVersion} ({settings.SpineMatch}).")
        };
    }

    private static async Task<string?> TryReadSpineVersionAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true);
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct).ConfigureAwait(false);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("skeleton", out var skeleton) &&
                skeleton.ValueKind == JsonValueKind.Object &&
                skeleton.TryGetProperty("spine", out var spine) &&
                spine.ValueKind == JsonValueKind.String)
            {
                return spine.GetString();
            }
        }
        catch (JsonException) { /* malformed or non-JSON — treat as not-Spine */ }
        catch (IOException) { /* unreadable — skip */ }
        return null;
    }

    /// <summary>Compare an actual version against a requirement, exactly or by major.minor.</summary>
    private static bool Matches(string actual, string required, SpineVersionMatch match)
    {
        if (match == SpineVersionMatch.Exact)
            return string.Equals(actual.Trim(), required.Trim(), StringComparison.OrdinalIgnoreCase);

        return MajorMinor(actual) == MajorMinor(required);

        static string MajorMinor(string v)
        {
            var parts = v.Trim().Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : v.Trim();
        }
    }
}
