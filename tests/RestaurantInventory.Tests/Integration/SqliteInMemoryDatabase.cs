using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Persistence;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// A real SQLite database held in memory for the lifetime of one test. The connection is
/// kept open so the schema survives between contexts; each <see cref="NewContext"/> is a
/// fresh <see cref="InventoryDbContext"/> over the same database — the way to prove that
/// state was actually persisted and reloaded, not just cached in one context's tracker.
/// </summary>
public sealed class SqliteInMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    public InventoryDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new InventoryDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
