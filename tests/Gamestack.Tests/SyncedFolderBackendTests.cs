using System.Text;
using Gamestack.Storage.SyncedFolder;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class SyncedFolderBackendTests
{
    [Fact]
    public async Task Upload_then_Download_round_trips_bytes()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);
        var payload = Encoding.UTF8.GetBytes("hello assets");

        var info = await backend.UploadAsync("art/hero.bin", new MemoryStream(payload));
        using var outStream = new MemoryStream();
        await backend.DownloadAsync("art/hero.bin", outStream);

        Assert.Equal(payload.Length, info.Size);
        Assert.Equal(payload, outStream.ToArray());
        Assert.True(File.Exists(dir.File("art/hero.bin")));
    }

    [Fact]
    public async Task List_returns_files_and_folders()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);
        await backend.UploadAsync("proj/a.txt", new MemoryStream([1, 2, 3]));
        Directory.CreateDirectory(dir.File("proj/sub"));

        var entries = await backend.ListAsync("proj");

        Assert.Contains(entries, e => e.Name == "a.txt" && e.Kind == Core.Models.RemoteEntryKind.File);
        Assert.Contains(entries, e => e.Name == "sub" && e.Kind == Core.Models.RemoteEntryKind.Folder);
    }

    [Fact]
    public async Task ReadText_returns_null_when_missing_and_round_trips_when_written()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);

        Assert.Null(await backend.ReadTextAsync(".gamestack/manifest.json"));
        await backend.WriteTextAsync(".gamestack/manifest.json", "{\"x\":1}");
        Assert.Equal("{\"x\":1}", await backend.ReadTextAsync(".gamestack/manifest.json"));
    }

    [Fact]
    public async Task DownloadVersion_is_not_supported()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);
        await Assert.ThrowsAsync<NotSupportedException>(
            () => backend.DownloadVersionAsync("a.txt", "1", new MemoryStream()));
    }

    [Fact]
    public async Task Path_escaping_the_root_is_rejected()
    {
        using var dir = new TempDir();
        var backend = new SyncedFolderBackend(dir.Path);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => backend.ReadTextAsync("../../etc/passwd"));
    }
}
