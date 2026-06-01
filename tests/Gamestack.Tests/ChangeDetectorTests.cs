using Gamestack.Core.Abstractions;
using Gamestack.Core.Models;
using Gamestack.Core.Sync;
using Gamestack.Core.Validation;
using Gamestack.Tests.Fakes;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class ChangeDetectorTests
{
    private static ChangeDetector NewDetector(out InMemoryLocalStateStore state)
    {
        state = new InMemoryLocalStateStore();
        var runner = new AssetValidationRunner(new IAssetValidator[] { new ImageDimensionValidator(), new SpineVersionValidator() });
        return new ChangeDetector(state, runner);
    }

    [Fact]
    public async Task File_without_baseline_is_Added()
    {
        using var dir = new TempDir();
        dir.WriteText("a.txt", "hello");
        var detector = NewDetector(out _);

        var changes = await detector.DetectAsync(dir.Path, new ValidationSettings());

        var change = Assert.Single(changes);
        Assert.Equal("a.txt", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
    }

    [Fact]
    public async Task Unchanged_file_with_matching_baseline_is_not_reported()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("a.txt", "hello");
        var detector = NewDetector(out var state);
        var sha = await Hasher.Sha256FileAsync(path);
        await state.SetBaselineAsync(new SyncBaseline("a.txt", 1, sha, 5, DateTimeOffset.UnixEpoch));

        Assert.Empty(await detector.DetectAsync(dir.Path, new ValidationSettings()));
    }

    [Fact]
    public async Task Modified_file_reports_sizes_and_baseline_version()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("a.txt", "hello");
        var detector = NewDetector(out var state);
        var sha = await Hasher.Sha256FileAsync(path);
        await state.SetBaselineAsync(new SyncBaseline("a.txt", 3, sha, 5, DateTimeOffset.UnixEpoch));

        File.WriteAllText(path, "hello world");
        var change = Assert.Single(await detector.DetectAsync(dir.Path, new ValidationSettings()));

        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(5, change.OldSize);
        Assert.Equal(11, change.NewSize);
        Assert.Equal(3, change.BaselineVersion);
    }

    [Fact]
    public async Task Baseline_without_local_file_is_Deleted()
    {
        using var dir = new TempDir();
        var detector = NewDetector(out var state);
        await state.SetBaselineAsync(new SyncBaseline("gone.txt", 2, "sha", 7, DateTimeOffset.UnixEpoch));

        var change = Assert.Single(await detector.DetectAsync(dir.Path, new ValidationSettings()));
        Assert.Equal(ChangeKind.Deleted, change.Kind);
        Assert.Equal("gone.txt", change.Path);
    }

    [Fact]
    public async Task Metadata_folder_is_ignored()
    {
        using var dir = new TempDir();
        dir.WriteText(".gamestack/manifest.json", "{}");
        var detector = NewDetector(out _);

        Assert.Empty(await detector.DetectAsync(dir.Path, new ValidationSettings()));
    }

    [Fact]
    public async Task Added_image_carries_validation_warnings()
    {
        using var dir = new TempDir();
        TestImages.WritePng(dir.File("sprite.png"), 100, 101);
        var detector = NewDetector(out _);

        var settings = new ValidationSettings { RequireSquare = true };
        var change = Assert.Single(await detector.DetectAsync(dir.Path, settings));

        Assert.Contains(change.Warnings, w => w.RuleId == "image.square");
    }
}
