using Gamestack.Core.Models;

namespace Gamestack.Core.Validation;

/// <summary>
/// Validates an asset against configured quality rules, producing zero or more findings.
/// Validators are registered in DI and run by the sync/change pipeline whenever a file is
/// added or updated. Implementations are independent and easy to extend — add a validator,
/// register it, done.
/// </summary>
public interface IAssetValidator
{
    /// <summary>Whether this validator applies to the given file (typically by extension/type).</summary>
    bool AppliesTo(string filePath);

    /// <summary>Validate the local file and return any findings.</summary>
    /// <param name="localPath">Absolute path to the local file.</param>
    /// <param name="settings">Current validation settings.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ValidationWarning>> ValidateAsync(string localPath, ValidationSettings settings, CancellationToken ct = default);
}
