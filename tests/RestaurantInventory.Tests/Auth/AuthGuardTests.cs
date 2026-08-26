using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace RestaurantInventory.Tests.Auth;

/// <summary>
/// The behavioural core of ticket 03: every inventory page requires auth, and hitting one
/// while unauthenticated redirects to Login. Driven end-to-end through the real app pipeline
/// (routing, auth middleware, the Blazor guard) via <see cref="WebApplicationFactory{T}"/> —
/// each factory boots against its own throwaway SQLite file so tests never touch the dev DB.
/// </summary>
public sealed class AuthGuardTests : IClassFixture<AuthGuardTests.AppFactory>
{
    private readonly AppFactory _factory;

    public AuthGuardTests(AppFactory factory) => _factory = factory;

    [Fact]
    public async Task ProtectedPage_WhileUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("login", response.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_IsReachableAnonymously()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/login");

        response.EnsureSuccessStatusCode();
        Assert.Contains("Sign in", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Boots the Web app against a unique temp SQLite file, deleted on dispose.</summary>
    public sealed class AppFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"ri-auth-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:Inventory", $"Data Source={_dbPath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            // SQLite pools the file handle; release it before deleting the temp DB. Best-effort:
            // a leftover temp file must never fail the test run.
            SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
