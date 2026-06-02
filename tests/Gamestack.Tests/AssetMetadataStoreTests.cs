using Gamestack.Core.Models;
using Gamestack.Core.Sync;
using Gamestack.Core.Versioning;
using Gamestack.Tests.Fakes;

namespace Gamestack.Tests;

public class AssetMetadataStoreTests
{
    private const string Root = "";
    private static readonly UserIdentity Alice = new("id-a", "Alice", "alice@studio.test");

    private static (AssetMetadataStore store, ManifestService manifests) NewStore(InMemoryStorageBackend backend)
    {
        var manifests = new ManifestService(new FixedClock(new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero)));
        return (new AssetMetadataStore(backend, manifests), manifests);
    }

    [Fact]
    public async Task Workspace_and_shards_round_trip()
    {
        var backend = new InMemoryStorageBackend();
        var (store, m) = NewStore(backend);

        var manifest = m.CreateNew("Demo", gameId: "42");
        m.AddVersion(manifest, "char/hero.psd", "sha1", 100, Alice, "init");
        m.AddVersion(manifest, "env/rock.png", "sha2", 200, Alice, "init");
        m.AddFileTag(manifest, "char/hero.psd", "Hero");
        m.SetAttribute(manifest, "char/hero.psd", "Artist", "Kris");

        await store.SaveWorkspaceAsync(Root, manifest);
        await store.SaveAssetAsync(Root, manifest, "char/hero.psd");
        await store.SaveAssetAsync(Root, manifest, "env/rock.png");

        // Fresh store (empty cache) re-assembles the manifest from disk.
        var (store2, _) = NewStore(backend);
        var loaded = await store2.LoadAsync(Root, "Demo");

        Assert.Equal("42", loaded.GameId);
        Assert.Contains("Hero", loaded.Tags);
        Assert.Contains(loaded.AttributeDefinitions, d => d.Key == "Artist");
        Assert.Equal(2, loaded.Files.Count);
        Assert.Equal(1, loaded.Files["char/hero.psd"].CurrentVersion);
        Assert.Equal(new[] { "Hero" }, loaded.Files["char/hero.psd"].Tags);
        Assert.Equal("Kris", loaded.Files["char/hero.psd"].Attributes["Artist"]);
        Assert.Equal("init", loaded.Files["env/rock.png"].Versions[0].Description);
    }

    [Fact]
    public async Task SaveAsset_writes_only_that_shard()
    {
        var backend = new InMemoryStorageBackend();
        var (store, m) = NewStore(backend);

        var manifest = m.CreateNew("Demo");
        m.AddVersion(manifest, "a.psd", "sha", 1, Alice, "i");
        m.AddVersion(manifest, "b.psd", "sha", 1, Alice, "i");

        await store.SaveAssetAsync(Root, manifest, "a.psd");

        Assert.NotNull(await backend.ReadTextAsync(AssetMetadataStore.ShardPath(Root, "a.psd")));
        Assert.Null(await backend.ReadTextAsync(AssetMetadataStore.ShardPath(Root, "b.psd")));
        // No single whole-workspace manifest is written by a per-asset save.
        Assert.Null(await backend.ReadTextAsync(AssetMetadataStore.LegacyManifestPath(Root)));
    }

    [Fact]
    public async Task Legacy_manifest_is_migrated_to_shards_on_load()
    {
        var backend = new InMemoryStorageBackend();
        var (store, m) = NewStore(backend);

        // Seed a pre-sharding manifest.json.
        var legacy = m.CreateNew("Demo", gameId: "7");
        m.AddVersion(legacy, "old/asset.png", "sha", 10, Alice, "legacy");
        m.AddFileTag(legacy, "old/asset.png", "Legacy");
        await backend.WriteTextAsync(AssetMetadataStore.LegacyManifestPath(Root), m.Serialize(legacy));

        var loaded = await store.LoadAsync(Root, "Demo");

        // Assembled correctly...
        Assert.Equal("7", loaded.GameId);
        Assert.Equal(1, loaded.Files["old/asset.png"].CurrentVersion);
        Assert.Contains("Legacy", loaded.Files["old/asset.png"].Tags);
        // ...and migrated to the sharded layout on disk.
        Assert.NotNull(await backend.ReadTextAsync(AssetMetadataStore.WorkspacePath(Root)));
        Assert.NotNull(await backend.ReadTextAsync(AssetMetadataStore.ShardPath(Root, "old/asset.png")));
    }
}
