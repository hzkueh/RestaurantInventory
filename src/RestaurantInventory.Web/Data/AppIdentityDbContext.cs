using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RestaurantInventory.Web.Data;

/// <summary>
/// Holds the ASP.NET Core Identity tables (users, roles, claims). Kept separate from
/// <c>InventoryDbContext</c> so the Core domain project stays free of any auth concern.
/// Both contexts live in the same SQLite file but own separate migrations-history tables
/// (see <see cref="MigrationsHistoryTableName"/>) so their migrations never collide.
/// </summary>
public class AppIdentityDbContext : IdentityDbContext<IdentityUser>
{
    /// <summary>The history table this context uses, distinct from Inventory's default one.</summary>
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory_Identity";

    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
    {
    }
}
