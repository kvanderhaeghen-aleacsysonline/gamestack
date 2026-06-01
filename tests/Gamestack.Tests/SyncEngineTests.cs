using Gamestack.Core.Models;
using Gamestack.Core.Sync;
using Gamestack.Core.Versioning;
using Gamestack.Tests.Fakes;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class SyncEngineTests
{
    private const string RemoteRoot = "projects/demo";
    private static readonly UserIdentity Alice = new("id-a", "Alice", "alice@studio.test");

    private static SyncEngine NewEngine(out InMemoryStorageBackend backend, out InMemoryLocalStateStore state, out ManifestService manifests)
    {
        backend = new InMemoryStorageBackend();
        state = new InMemoryLocalStateStore();
        manifests = new ManifestService(new FixedClock(new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero)));
        return new SyncEngine(backend, state, manifests);
    }

    [Fact]
    public async Task Push_uploads_assigns_v1_persists_manifest_and_baseline()
    {
        var engine = NewEngine(out var backend, out var state, out _);
        using var dir = new TempDir();
        dir.WriteText("hero.psd", "pixels");
        var manifest = await engine.LoadManifestAsync(RemoteRoot, "Demo");

        var result = await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "first", Alice);

        Assert.False(result.Conflict);
        Assert.Equal(1, result.Version!.Version);
        Assert.Equal(1, backend.UploadCount);
        Assert.Equal(1, manifest.Files["hero.psd"].CurrentVersion);
        Assert.Equal("first", manifest.Files["hero.psd"].Versions[0].Description);

        var baseline = await state.GetBaselineAsync("hero.psd");
        Assert.Equal(1, baseline!.Version);
        Assert.NotNull(await backend.ReadTextAsync(SyncEngine.ManifestPath(RemoteRoot)));
    }

    [Fact]
    public async Task Second_push_after_edit_assigns_v2()
    {
        var engine = NewEngine(out _, out _, out _);
        using var dir = new TempDir();
        var path = dir.WriteText("hero.psd", "pixels");
        var manifest = await engine.LoadManifestAsync(RemoteRoot, "Demo");

        await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "first", Alice);
        File.WriteAllText(path, "more pixels");
        var second = await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "second", Alice);

        Assert.Equal(2, second.Version!.Version);
    }

    [Fact]
    public async Task Push_is_refused_as_conflict_when_remote_advanced()
    {
        var engine = NewEngine(out var backend, out _, out var manifests);
        using var dir = new TempDir();
        dir.WriteText("hero.psd", "pixels");
        var manifest = await engine.LoadManifestAsync(RemoteRoot, "Demo");

        await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "first", Alice); // baseline -> v1
        manifests.AddVersion(manifest, "hero.psd", "othersha", 1, Alice, "someone else"); // remote -> v2, baseline still 1
        var uploadsBefore = backend.UploadCount;

        var conflict = await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "mine", Alice);

        Assert.True(conflict.Conflict);
        Assert.Equal(1, conflict.BaselineVersion);
        Assert.Equal(2, conflict.RemoteVersion);
        Assert.Equal(uploadsBefore, backend.UploadCount); // nothing uploaded
    }

    [Fact]
    public async Task Forced_push_overrides_conflict()
    {
        var engine = NewEngine(out _, out _, out var manifests);
        using var dir = new TempDir();
        dir.WriteText("hero.psd", "pixels");
        var manifest = await engine.LoadManifestAsync(RemoteRoot, "Demo");

        await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "first", Alice);
        manifests.AddVersion(manifest, "hero.psd", "othersha", 1, Alice, "someone else");

        var forced = await engine.PushFileAsync(RemoteRoot, dir.Path, "hero.psd", manifest, "mine", Alice, force: true);

        Assert.False(forced.Conflict);
        Assert.Equal(3, forced.Version!.Version);
    }

    [Fact]
    public async Task Download_writes_file_and_records_materialized_baseline()
    {
        var engine = NewEngine(out var backend, out var state, out _);
        using var dir = new TempDir();
        await backend.UploadAsync($"{RemoteRoot}/art.png", new MemoryStream(TestImages.Png(64, 64)));

        var manifest = await engine.LoadManifestAsync(RemoteRoot, "Demo");
        manifest.Files["art.png"] = new AssetFile { CurrentVersion = 5 };

        await engine.DownloadFileAsync(RemoteRoot, dir.Path, "art.png", manifest);

        Assert.True(File.Exists(dir.File("art.png")));
        Assert.True(await state.IsMaterializedAsync("art.png"));
        var baseline = await state.GetBaselineAsync("art.png");
        Assert.Equal(5, baseline!.Version);
    }
}
