using Gamestack.Core.Models;
using Gamestack.Core.Validation;
using Gamestack.Tests.Support;

namespace Gamestack.Tests;

public class ValidationTests
{
    // ---- Image dimension rules ----

    [Fact]
    public async Task Square_and_divisible_by_four_pass_for_clean_dimensions()
    {
        using var dir = new TempDir();
        var path = dir.File("ok.png");
        TestImages.WritePng(path, 256, 256);

        var settings = new ValidationSettings { RequireSquare = true, RequireDivisibleByFour = true, RequirePowerOfTwo = true };
        var warnings = await new ImageDimensionValidator().ValidateAsync(path, settings);

        Assert.Empty(warnings);
    }

    [Fact]
    public async Task Non_square_non_div4_non_pow2_image_produces_three_warnings()
    {
        using var dir = new TempDir();
        var path = dir.File("bad.png");
        TestImages.WritePng(path, 100, 101);

        var settings = new ValidationSettings { RequireSquare = true, RequireDivisibleByFour = true, RequirePowerOfTwo = true };
        var warnings = await new ImageDimensionValidator().ValidateAsync(path, settings);

        Assert.Contains(warnings, w => w.RuleId == "image.square");
        Assert.Contains(warnings, w => w.RuleId == "image.divisibleByFour");
        Assert.Contains(warnings, w => w.RuleId == "image.powerOfTwo");
    }

    [Fact]
    public async Task BlockOnImageFailure_promotes_warnings_to_errors()
    {
        using var dir = new TempDir();
        var path = dir.File("bad.png");
        TestImages.WritePng(path, 100, 100); // square + div4, but not pow2

        var settings = new ValidationSettings { RequirePowerOfTwo = true, BlockOnImageFailure = true };
        var warnings = await new ImageDimensionValidator().ValidateAsync(path, settings);

        var w = Assert.Single(warnings);
        Assert.Equal("image.powerOfTwo", w.RuleId);
        Assert.Equal(ValidationSeverity.Error, w.Severity);
    }

    // ---- Spine version rule ----

    private static string SpineJson(string version) => $$"""{ "skeleton": { "spine": "{{version}}" }, "bones": [] }""";

    [Fact]
    public async Task Spine_major_minor_mismatch_warns()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("skel.json", SpineJson("4.0.15"));

        var settings = new ValidationSettings { CheckSpineVersion = true, RequiredSpineVersion = "4.1", SpineMatch = SpineVersionMatch.MajorMinor };
        var warnings = await new SpineVersionValidator().ValidateAsync(path, settings);

        Assert.Single(warnings, w => w.RuleId == "spine.version");
    }

    [Fact]
    public async Task Spine_major_minor_match_passes()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("skel.json", SpineJson("4.0.15"));

        var settings = new ValidationSettings { CheckSpineVersion = true, RequiredSpineVersion = "4.0", SpineMatch = SpineVersionMatch.MajorMinor };
        var warnings = await new SpineVersionValidator().ValidateAsync(path, settings);

        Assert.Empty(warnings);
    }

    [Fact]
    public async Task Spine_exact_match_is_strict()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("skel.json", SpineJson("4.0.15"));
        var settings = new ValidationSettings { CheckSpineVersion = true, RequiredSpineVersion = "4.0.14", SpineMatch = SpineVersionMatch.Exact };

        Assert.Single(await new SpineVersionValidator().ValidateAsync(path, settings));
    }

    [Fact]
    public async Task Non_spine_json_is_ignored()
    {
        using var dir = new TempDir();
        var path = dir.WriteText("data.json", """{ "foo": 1, "bar": [2,3] }""");

        var settings = new ValidationSettings { CheckSpineVersion = true, RequiredSpineVersion = "4.1" };
        var warnings = await new SpineVersionValidator().ValidateAsync(path, settings);

        Assert.Empty(warnings);
    }
}
