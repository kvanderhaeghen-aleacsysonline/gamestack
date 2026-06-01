using Gamestack.Core.Projects;
using Gamestack.Storage.SyncedFolder;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class GameLinkTests
{
    private static SyncedFolderBackend BackendWithDirs(TempDir dir, params string[] names)
    {
        foreach (var name in names)
            Directory.CreateDirectory(dir.File(name));
        return new SyncedFolderBackend(dir.Path);
    }

    [Fact]
    public async Task EnsureMarkers_creates_gamestack_json_with_folder_name_as_gameId()
    {
        using var dir = new TempDir();
        var backend = BackendWithDirs(dir, "CosmicSlots", "PirateBay");
        var service = new GameLinkService();

        var created = await service.EnsureMarkersAsync(backend);

        Assert.Equal(2, created);
        Assert.True(File.Exists(dir.File("CosmicSlots/gamestack.json")));
        var link = await service.ReadAsync(backend, "CosmicSlots");
        Assert.Equal("CosmicSlots", link!.GameId);
    }

    [Fact]
    public async Task EnsureMarkers_is_idempotent_and_preserves_edited_gameId()
    {
        using var dir = new TempDir();
        var backend = BackendWithDirs(dir, "CosmicSlots");
        var service = new GameLinkService();

        await service.EnsureMarkersAsync(backend);
        await service.WriteAsync(backend, "CosmicSlots", new GameLink { GameId = "cosmic-slots-42" });

        var createdSecondPass = await service.EnsureMarkersAsync(backend);

        Assert.Equal(0, createdSecondPass); // nothing missing
        var link = await service.ReadAsync(backend, "CosmicSlots");
        Assert.Equal("cosmic-slots-42", link!.GameId); // edit not clobbered
    }

    [Fact]
    public async Task CreateGame_makes_folder_and_marker_with_name_as_gameId()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);
        var service = new GameLinkService();

        await service.CreateGameAsync(backend, "NewGame");

        Assert.True(Directory.Exists(dir.File("NewGame")));
        Assert.Equal("NewGame", (await service.ReadAsync(backend, "NewGame"))!.GameId);
    }

    [Fact]
    public async Task CreateGame_rejects_duplicates_and_invalid_names()
    {
        using var dir = new TempDir();
        var backend = BackendWithDirs(dir, "Existing");
        var service = new GameLinkService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateGameAsync(backend, "Existing"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateGameAsync(backend, "bad/name"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateGameAsync(backend, "  "));
    }

    [Fact]
    public async Task Scan_reports_missing_versus_linked_directories()
    {
        using var dir = new TempDir();
        var backend = BackendWithDirs(dir, "A", "B");
        var service = new GameLinkService();
        await service.WriteAsync(backend, "A", new GameLink { GameId = "game-a" });

        var scan = await service.ScanAsync(backend);
        Assert.Equal(2, scan.Count);
        Assert.Equal(1, await service.CountMissingAsync(backend));
        Assert.Equal("game-a", scan.Single(p => p.Name == "A").Link!.GameId);
        Assert.Null(scan.Single(p => p.Name == "B").Link);
    }
}
