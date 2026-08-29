using RestaurantInventory.Web.Infrastructure;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// The <c>.env</c> parsing rules that make the git-ignored key reachable as configuration
/// (<c>Gemini__ApiKey</c> → <c>Gemini:ApiKey</c>), covered without a file on disk.
/// </summary>
public sealed class DotEnvTests
{
    private static Dictionary<string, string?> ParseToMap(params string[] lines)
        => DotEnv.Parse(lines).ToDictionary(kv => kv.Key, kv => kv.Value);

    [Fact]
    public void Parse_MapsDoubleUnderscoreToColon()
    {
        var map = ParseToMap("Gemini__ApiKey=abc123");

        Assert.Equal("abc123", map["Gemini:ApiKey"]);
    }

    [Fact]
    public void Parse_SkipsBlankLinesAndComments()
    {
        var map = ParseToMap("", "# a comment", "   ", "Gemini__ModelId=gemini-3.1-flash-lite");

        Assert.Single(map);
        Assert.Equal("gemini-3.1-flash-lite", map["Gemini:ModelId"]);
    }

    [Fact]
    public void Parse_StripsSurroundingQuotes()
    {
        var map = ParseToMap("Gemini__ApiKey=\"quoted key\"", "Other__Value='single'");

        Assert.Equal("quoted key", map["Gemini:ApiKey"]);
        Assert.Equal("single", map["Other:Value"]);
    }

    [Fact]
    public void Parse_IgnoresLinesWithoutAKey()
    {
        Assert.Empty(ParseToMap("=novalue", "justtext"));
    }

    [Fact]
    public void Parse_DropsALeadingExportPrefix()
    {
        var map = ParseToMap("export Gemini__ApiKey=abc123");

        Assert.Equal("abc123", map["Gemini:ApiKey"]);
    }

    [Fact]
    public void Parse_StripsAnUnquotedTrailingComment_ButKeepsAHashInsideTheValue()
    {
        var map = ParseToMap("Gemini__ApiKey=abc123  # my key", "Gemini__ModelId=models/gpt#5");

        Assert.Equal("abc123", map["Gemini:ApiKey"]);
        Assert.Equal("models/gpt#5", map["Gemini:ModelId"]); // no leading space → part of the value
    }
}
