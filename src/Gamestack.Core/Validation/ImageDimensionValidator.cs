using Gamestack.Core.Models;

namespace Gamestack.Core.Validation;

/// <summary>
/// Validates raster image dimensions (PNG/JPG/BMP/WEBP) against the configured rules:
/// divisible-by-4, square, and power-of-two. Reads only the image header via
/// <see cref="ImageDimensionReader"/> — it never decodes pixels and has no third-party dependency.
/// </summary>
public sealed class ImageDimensionValidator : IAssetValidator
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };

    /// <inheritdoc />
    public bool AppliesTo(string filePath) => Extensions.Contains(Path.GetExtension(filePath));

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationWarning>> ValidateAsync(
        string localPath, ValidationSettings settings, CancellationToken ct = default)
    {
        if (!settings.Enabled ||
            (!settings.RequireDivisibleByFour && !settings.RequireSquare && !settings.RequirePowerOfTwo))
        {
            return Array.Empty<ValidationWarning>();
        }

        await Task.Yield(); // keep the async signature; header read is fast/synchronous
        if (!ImageDimensionReader.TryReadDimensions(localPath, out int w, out int h))
            return Array.Empty<ValidationWarning>();

        var severity = settings.BlockOnImageFailure ? ValidationSeverity.Error : ValidationSeverity.Warning;
        var warnings = new List<ValidationWarning>();

        if (settings.RequireDivisibleByFour && (w % 4 != 0 || h % 4 != 0))
            warnings.Add(new("image.divisibleByFour", severity, $"{w}×{h} is not divisible by 4."));

        if (settings.RequireSquare && w != h)
            warnings.Add(new("image.square", severity, $"{w}×{h} is not square."));

        if (settings.RequirePowerOfTwo && (!IsPowerOfTwo(w) || !IsPowerOfTwo(h)))
            warnings.Add(new("image.powerOfTwo", severity, $"{w}×{h} is not a power of two."));

        return warnings;
    }

    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}
