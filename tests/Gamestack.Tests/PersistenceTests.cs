using Gamestack.Core.Abstractions;
using Gamestack.Core.Settings;
using Gamestack.Infrastructure;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class PersistenceTests
{
    [Fact]
    public async Task LocalStateStore_persists_baselines_and_materialized_across_instances()
    {
        using var dir = new TempDir();
        var path = dir.File("state.json");

        var store = new JsonLocalStateStore(path);
        await store.SetBaselineAsync(new SyncBaseline("a/b.png", 2, "sha", 100, DateTimeOffset.UnixEpoch));
        await store.SetMaterializedAsync("a/b.png", true);

        var reloaded = new JsonLocalStateStore(path);
        var baseline = await reloaded.GetBaselineAsync("a/b.png");

        Assert.Equal(2, baseline!.Version);
        Assert.Equal("sha", baseline.Sha256);
        Assert.True(await reloaded.IsMaterializedAsync("a/b.png"));
    }

    [Fact]
    public async Task SettingsStore_returns_defaults_when_missing_and_round_trips_when_saved()
    {
        using var dir = new TempDir();
        var path = dir.File("settings.json");
        var store = new JsonSettingsStore(path);

        Assert.Equal("17:30", (await store.LoadAsync()).EndOfDayReminderTime); // defaults

        await store.SaveAsync(new AppSettings
        {
            SyncedFolderRoot = @"C:\OneDrive\Assets",
            WorkspaceRoot = @"D:\Work",
            Validation = { RequireSquare = true, CheckSpineVersion = true, RequiredSpineVersion = "4.1" },
            Projects = { new ProjectLink { Name = "Cosmic", RemotePath = "cosmic", GameSlug = "cosmic-slots" } },
        });

        var loaded = await store.LoadAsync();
        Assert.Equal(@"C:\OneDrive\Assets", loaded.SyncedFolderRoot);
        Assert.True(loaded.Validation.RequireSquare);
        Assert.Equal("4.1", loaded.Validation.RequiredSpineVersion);
        Assert.Single(loaded.Projects);
        Assert.Equal("cosmic-slots", loaded.Projects[0].GameSlug);
    }
}
