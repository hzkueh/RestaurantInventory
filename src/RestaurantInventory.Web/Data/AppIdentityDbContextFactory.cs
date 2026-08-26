using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantInventory.Web.Data;

/// <summary>
/// Lets the EF Core CLI build <see cref="AppIdentityDbContext"/> at design time
/// (migrations add / database update) without booting the Web host. The connection
/// string here is only used by tooling; the running app supplies its own.
/// </summary>
public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlite(
                "Data Source=restaurantinventory.db",
                sqlite => sqlite.MigrationsHistoryTable(AppIdentityDbContext.MigrationsHistoryTableName))
            .Options;

        return new AppIdentityDbContext(options);
    }
}
