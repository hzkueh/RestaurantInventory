using RestaurantInventory.Web.Infrastructure;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// Parsing the Gemini response is the only piece of the HTTP path with real logic, so it is pinned
/// here as a pure function of the raw JSON — the live endpoint is never called in the suite
/// (ADR-0002). A malformed or empty response must yield <c>null</c> (→ a graceful "unavailable"),
/// never an exception.
/// </summary>
public sealed class GeminiResponseReaderTests
{
    [Fact]
    public void ReadNarrative_ReturnsTheFirstCandidatesText()
    {
        const string json = """
            { "candidates": [ { "content": { "parts": [ { "text": "Tomatoes are low." } ] } } ] }
            """;

        Assert.Equal("Tomatoes are low.", GeminiResponseReader.ReadNarrative(json));
    }

    [Fact]
    public void ReadNarrative_ConcatenatesMultipleParts()
    {
        const string json = """
            { "candidates": [ { "content": { "parts": [ { "text": "One. " }, { "text": "Two." } ] } } ] }
            """;

        Assert.Equal("One. Two.", GeminiResponseReader.ReadNarrative(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{ \"candidates\": [] }")]
    [InlineData("{ \"candidates\": [ { \"content\": { \"parts\": [] } } ] }")]
    [InlineData("{ \"candidates\": [ { \"content\": { \"parts\": [ { \"text\": \"   \" } ] } } ] }")]
    public void ReadNarrative_ReturnsNullForUnusableResponses(string json)
    {
        Assert.Null(GeminiResponseReader.ReadNarrative(json));
    }
}
