using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Validation;

namespace Gamestack.Core.Sync;

/// <summary>
/// Detects which materialized files differ from their last-synced baseline and attaches
/// validation findings to added/modified files. Drives the Changes view ("ready to push").
/// </summary>
public sealed class ChangeDetector
{
    /// <summary>Folder name reserved for Gamestack metadata; never reported as a change.</summary>
    public const string MetadataFolder = ".gamestack";

    private readonly ILocalStateStore _state;
    private readonly AssetValidationRunner _validation;

    /// <summary>Create the detector over the local state store and validation runner.</summary>
    public ChangeDetector(ILocalStateStore state, AssetValidationRunner validation)
    {
        _state = state;
        _validation = validation;
    }

    /// <summary>
    /// Scan <paramref name="localRoot"/> and return the pending changes. Files present locally but
    /// absent from the baseline are <see cref="ChangeKind.Added"/>; differing hashes are
    /// <see cref="ChangeKind.Modified"/>; baselines with no local file are <see cref="ChangeKind.Deleted"/>.
    /// </summary>
    public async Task<IReadOnlyList<FileChange>> DetectAsync(
        string localRoot, ValidationSettings validation, CancellationToken ct = default)
    {
        var baselines = (await _state.GetAllBaselinesAsync(ct).ConfigureAwait(false))
            .ToDictionary(b => b.Path, StringComparer.Ordinal);

        var changes = new List<FileChange>();

        if (Directory.Exists(localRoot))
        {
            foreach (var absolute in Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var rel = ToRelative(localRoot, absolute);
                if (IsIgnored(rel))
                    continue;

                var size = new FileInfo(absolute).Length;
                var sha = await Hasher.Sha256FileAsync(absolute, ct).ConfigureAwait(false);

                if (baselines.TryGetValue(rel, out var baseline))
                {
                    baselines.Remove(rel);
                    if (!string.Equals(baseline.Sha256, sha, StringComparison.Ordinal))
                    {
                        changes.Add(new FileChange
                        {
                            Path = rel,
                            Kind = ChangeKind.Modified,
                            OldSize = baseline.Size,
                            NewSize = size,
                            BaselineVersion = baseline.Version,
                            Warnings = await _validation.RunAsync(absolute, validation, ct).ConfigureAwait(false),
                        });
                    }
                }
                else
                {
                    changes.Add(new FileChange
                    {
                        Path = rel,
                        Kind = ChangeKind.Added,
                        OldSize = 0,
                        NewSize = size,
                        BaselineVersion = 0,
                        Warnings = await _validation.RunAsync(absolute, validation, ct).ConfigureAwait(false),
                    });
                }
            }
        }

        // Baselines with no corresponding local file were deleted.
        foreach (var orphan in baselines.Values)
        {
            changes.Add(new FileChange
            {
                Path = orphan.Path,
                Kind = ChangeKind.Deleted,
                OldSize = orphan.Size,
                NewSize = 0,
                BaselineVersion = orphan.Version,
            });
        }

        return changes;
    }

    private static bool IsIgnored(string relativePath) =>
        relativePath.Equals(MetadataFolder, StringComparison.OrdinalIgnoreCase) ||
        relativePath.StartsWith(MetadataFolder + "/", StringComparison.OrdinalIgnoreCase);

    private static string ToRelative(string root, string absolute) =>
        Path.GetRelativePath(root, absolute).Replace('\\', '/');
}
