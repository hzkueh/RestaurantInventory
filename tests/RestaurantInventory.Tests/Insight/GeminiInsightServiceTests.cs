using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Tests.Integration;
using RestaurantInventory.Web.Infrastructure;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// The no-key short-circuit of <see cref="GeminiInsightService"/> — the concrete graceful path for
/// user story 29. With no key configured the service must report "unavailable" without contacting
/// the provider at all, so a missing key never puts the network on the path (and the live endpoint
/// is never called in the suite, per ADR-0002).
/// </summary>
public sealed class GeminiInsightServiceTests
{
    /// <summary>Fails the test if any HTTP request is attempted — proves the no-key path stays offline.</summary>
    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException("The provider must not be called when no key is configured.");
    }

    [Fact]
    public async Task SummariseAsync_WithNoKey_ReportsUnavailableWithoutCallingTheProvider()
    {
        using var db = new SqliteInMemoryDatabase();
        using var http = new HttpClient(new ThrowingHandler()) { BaseAddress = new Uri("https://example.invalid/") };
        var inventory = new InventoryService(db.NewContext());
        var options = Options.Create(new GeminiOptions { ApiKey = null });

        var service = new GeminiInsightService(http, inventory, options, NullLogger<GeminiInsightService>.Instance);

        var result = await service.SummariseAsync();

        Assert.False(result.IsAvailable);
        Assert.Null(result.Briefing);
        Assert.Contains("no AI provider key", result.UnavailableReason);
    }
}
