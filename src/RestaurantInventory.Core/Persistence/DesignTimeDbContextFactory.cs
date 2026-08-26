using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantInventory.Core.Persistence;

/// <summary>
/// Lets the EF Core CLI (migrations add / database update) build the context at
/// design time without booting the Web host. The connection string here is only
/// used by tooling; the running app supplies its own.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite("Data Source=restaurantinventory.db")
            .Options;

        return new InventoryDbContext(options);
    }
}
