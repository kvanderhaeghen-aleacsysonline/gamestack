using Gamestack.Core.Models;
using Gamestack.Core.Organization;
using Gamestack.Core.Versioning;
using Gamestack.Tests.Fakes;

namespace Gamestack.Tests;

public class OrganizationTests
{
    private static readonly UserIdentity Alice = new("id-a", "Alice", "alice@studio.test");

    private static ManifestService NewService() =>
        new(new FixedClock(new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Tokenize_splits_on_non_alphanumeric_and_lowercases()
    {
        var tokens = FileNameTokenizer.Tokenize("Red_sword-3.png");
        Assert.Equal(new[] { "red", "sword", "3", "png" }, tokens);
    }

    [Fact]
    public void MatchTags_matches_vocabulary_case_insensitively_preserving_casing()
    {
        var matched = FileNameTokenizer.MatchTags("Red_sword-3.png", new[] { "Red", "Sword", "Shield" });
        Assert.Equal(new[] { "Red", "Sword" }, matched);
    }

    [Fact]
    public void AutoTagFile_adds_matching_vocabulary_tags()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo");
        svc.AddTagToVocabulary(m, "Red");
        svc.AddTagToVocabulary(m, "Sword");
        svc.AddVersion(m, "weapons/Red_sword-3.png", "sha", 10, Alice, "init");

        var added = svc.AutoTagFile(m, "weapons/Red_sword-3.png");

        Assert.Equal(new[] { "Red", "Sword" }, added);
        Assert.Equal(new[] { "Red", "Sword" }, m.Files["weapons/Red_sword-3.png"].Tags);
    }

    [Fact]
    public void AddFileTag_dedupes_and_seeds_vocabulary()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo");

        Assert.True(svc.AddFileTag(m, "a.png", "Hero"));
        Assert.False(svc.AddFileTag(m, "a.png", "hero")); // case-insensitive dupe
        Assert.Single(m.Files["a.png"].Tags);
        Assert.Contains("Hero", m.Tags);
    }

    [Fact]
    public void SetAttribute_defines_then_sets_and_clears()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo");

        svc.SetAttribute(m, "a.png", "Artist", "Kris");
        Assert.Equal("Kris", m.Files["a.png"].Attributes["Artist"]);
        Assert.Contains(m.AttributeDefinitions, d => d.Key == "Artist");

        svc.SetAttribute(m, "a.png", "Artist", null);
        Assert.False(m.Files["a.png"].Attributes.ContainsKey("Artist"));
    }

    [Fact]
    public void Search_filters_by_text_tag_extension_and_attribute()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo", gameId: "42");
        svc.AddVersion(m, "char/hero.png", "s1", 1, Alice, "i");
        svc.AddVersion(m, "char/hero.psd", "s2", 1, Alice, "i");
        svc.AddVersion(m, "env/rock.png", "s3", 1, Alice, "i");
        svc.AddFileTag(m, "char/hero.png", "Hero");
        svc.SetAttribute(m, "char/hero.png", "Artist", "Kris");

        Assert.Equal(2, AssetSearch.Search(m, new SearchQuery { Text = "char" }).Count);
        Assert.Equal(2, AssetSearch.Search(m, new SearchQuery { Extension = "png" }).Count);

        var byTag = AssetSearch.Search(m, new SearchQuery { Tags = new[] { "hero" } });
        Assert.Equal("char/hero.png", Assert.Single(byTag).Path);

        var byAttr = AssetSearch.Search(m, new SearchQuery
        {
            Attributes = new Dictionary<string, string> { ["Artist"] = "kris" },
        });
        Assert.Equal("char/hero.png", Assert.Single(byAttr).Path);
    }

    [Fact]
    public void Search_by_gameId_matches_manifest_game()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo", gameId: "42");
        svc.AddVersion(m, "a.png", "s", 1, Alice, "i");

        Assert.Single(AssetSearch.Search(m, new SearchQuery { GameId = "42" }));
        Assert.Empty(AssetSearch.Search(m, new SearchQuery { GameId = "99" }));
    }

    [Fact]
    public void Tags_and_attributes_round_trip_through_json()
    {
        var svc = NewService();
        var m = svc.CreateNew("Demo");
        svc.AddVersion(m, "a.png", "s", 1, Alice, "i");
        svc.AddFileTag(m, "a.png", "Hero");
        svc.DefineAttribute(m, "Approved", AttributeValueType.Boolean);
        svc.SetAttribute(m, "a.png", "Approved", "true");

        var restored = svc.Deserialize(svc.Serialize(m));

        Assert.Contains("Hero", restored.Files["a.png"].Tags);
        Assert.Equal("true", restored.Files["a.png"].Attributes["Approved"]);
        Assert.Contains(restored.AttributeDefinitions, d => d.Key == "Approved" && d.Type == AttributeValueType.Boolean);
    }
}
