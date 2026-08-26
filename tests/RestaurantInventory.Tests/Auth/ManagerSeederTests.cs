using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantInventory.Web.Data;

namespace RestaurantInventory.Tests.Auth;

/// <summary>
/// The seeded Manager (user stories 1–2) is what the whole app authenticates as, so it is
/// worth proving at the seam we own: the seeder creates exactly one Manager whose password
/// verifies, and running it again is a no-op. Exercised against a real Identity store over
/// in-memory SQLite, not a mock, so the password hashing and uniqueness rules are real.
/// </summary>
public sealed class ManagerSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public ManagerSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AppIdentityDbContext>();
        _services = services.BuildServiceProvider();

        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.EnsureCreated();
    }

    private static readonly ManagerSeedOptions Options = new()
    {
        Email = "manager@restaurant.local",
        Password = "Manager!1",
    };

    [Fact]
    public async Task SeedAsync_CreatesManager_WhoseConfiguredPasswordVerifies()
    {
        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await ManagerSeeder.SeedAsync(userManager, Options);

        var manager = await userManager.FindByEmailAsync(Options.Email);
        Assert.NotNull(manager);
        Assert.True(await userManager.CheckPasswordAsync(manager!, Options.Password));
    }

    [Fact]
    public async Task SeedAsync_RejectsTheWrongPassword()
    {
        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await ManagerSeeder.SeedAsync(userManager, Options);

        var manager = await userManager.FindByEmailAsync(Options.Email);
        Assert.False(await userManager.CheckPasswordAsync(manager!, "not-the-password"));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_LeavingASingleManager()
    {
        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var first = await ManagerSeeder.SeedAsync(userManager, Options);
        var second = await ManagerSeeder.SeedAsync(userManager, Options);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, userManager.Users.Count(u => u.Email == Options.Email));
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}
