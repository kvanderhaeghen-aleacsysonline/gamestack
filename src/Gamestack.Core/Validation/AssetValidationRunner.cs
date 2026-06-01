using Gamestack.Core.Models;

namespace Gamestack.Core.Validation;

/// <summary>
/// Runs all registered <see cref="IAssetValidator"/>s applicable to a file and aggregates their
/// findings. Registered in DI with the full set of validators; new rules are added simply by
/// registering another validator.
/// </summary>
public sealed class AssetValidationRunner
{
    private readonly IReadOnlyList<IAssetValidator> _validators;

    /// <summary>Create the runner over the registered validators.</summary>
    public AssetValidationRunner(IEnumerable<IAssetValidator> validators)
        => _validators = validators.ToList();

    /// <summary>Run every applicable validator and return the combined findings.</summary>
    public async Task<IReadOnlyList<ValidationWarning>> RunAsync(
        string localPath, ValidationSettings settings, CancellationToken ct = default)
    {
        if (!settings.Enabled)
            return Array.Empty<ValidationWarning>();

        var findings = new List<ValidationWarning>();
        foreach (var validator in _validators)
        {
            if (!validator.AppliesTo(localPath))
                continue;
            findings.AddRange(await validator.ValidateAsync(localPath, settings, ct).ConfigureAwait(false));
        }
        return findings;
    }
}
