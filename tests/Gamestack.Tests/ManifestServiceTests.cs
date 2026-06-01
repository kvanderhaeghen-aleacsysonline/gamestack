using Gamestack.Core.Models;
using Gamestack.Core.Versioning;
using Gamestack.Tests.Fakes;

namespace Gamestack.Tests;

public class ManifestServiceTests
{
    private static readonly UserIdentity Alice = new("id-a", "Alice", "alice@studio.test");

    private static ManifestService NewService(out FixedClock clock)
    {
        clock = new FixedClock(new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero));
        return new ManifestService(clock);
    }

    [Fact]
    public void AddVersion_increments_and_stamps_author_and_time()
    {
        var svc = NewService(out var clock);
        var m = svc.CreateNew("Demo");

        var v1 = svc.AddVersion(m, "hero.psd", "sha1", 10, Alice, "first");
        var v2 = svc.AddVersion(m, "hero.psd", "sha2", 20, Alice, "second");

        Assert.Equal(1, v1.Version);
        Assert.Equal(2, v2.Version);
        Assert.Equal(2, m.Files["hero.psd"].CurrentVersion);
        Assert.Equal(2, m.Files["hero.psd"].Versions.Count);
        Assert.Equal(Alice, v1.PushedBy);
        Assert.Equal(clock.UtcNow, v1.PushedAtUtc);
    }

    [Fact]
    public void AddComment_records_author_kind_and_version()
    {
        var svc = NewService(out _);
        var m = svc.CreateNew("Demo");

        var c = svc.AddComment(m, "hero.psd", Alice, "soften highlights", version: 3, kind: CommentKind.RequestChanges);

        Assert.Single(m.Files["hero.psd"].Comments);
        Assert.Equal(CommentKind.RequestChanges, c.Kind);
        Assert.Equal(3, c.Version);
        Assert.False(string.IsNullOrWhiteSpace(c.Id));
    }

    [Fact]
    public void Serialize_then_Deserialize_round_trips()
    {
        var svc = NewService(out _);
        var m = svc.CreateNew("Demo", gameSlug: "cosmic-slots", gameId: "42");
        svc.AddVersion(m, "a/b.png", "sha", 99, Alice, "init");
        svc.AddComment(m, "a/b.png", Alice, "looks good", kind: CommentKind.Approve);

        var restored = svc.Deserialize(svc.Serialize(m));

        Assert.Equal("cosmic-slots", restored.GameSlug);
        Assert.Equal(1, restored.Files["a/b.png"].CurrentVersion);
        Assert.Equal(CommentKind.Approve, restored.Files["a/b.png"].Comments[0].Kind);
        Assert.Equal("alice@studio.test", restored.Files["a/b.png"].Versions[0].PushedBy.Email);
    }
}
