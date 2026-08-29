using RestaurantInventory.Web.Infrastructure;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// The provider is "configured" only when an API key is present — this is the switch behind the
/// graceful "unavailable" path (user story 29). The model id falls back to the ADR-0002 default.
/// </summary>
public sealed class GeminiOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsConfigured_IsFalse_WithoutAnApiKey(string? apiKey)
    {
        Assert.False(new GeminiOptions { ApiKey = apiKey }.IsConfigured);
    }

    [Fact]
    public void IsConfigured_IsTrue_WithAnApiKey()
    {
        Assert.True(new GeminiOptions { ApiKey = "a-key" }.IsConfigured);
    }

    [Fact]
    public void ResolvedModelId_FallsBackToTheDefault_WhenUnset()
    {
        Assert.Equal(GeminiOptions.DefaultModelId, new GeminiOptions().ResolvedModelId);
        Assert.Equal(GeminiOptions.DefaultModelId, new GeminiOptions { ModelId = "  " }.ResolvedModelId);
    }

    [Fact]
    public void ResolvedModelId_UsesTheConfiguredModel_WhenSet()
    {
        Assert.Equal("some-other-model", new GeminiOptions { ModelId = "some-other-model" }.ResolvedModelId);
    }
}
