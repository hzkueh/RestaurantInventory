using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Core.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(item =>
        {
            item.HasKey(i => i.Id);
            item.Property(i => i.Name).IsRequired().HasMaxLength(200);
            // Enums persisted by name so the schema stays readable and stable.
            item.Property(i => i.UnitOfMeasure).HasConversion<string>().HasMaxLength(20).IsRequired();
            item.Property(i => i.UnitCost).HasPrecision(18, 2);
            item.Property(i => i.ReorderLevel).HasPrecision(18, 3);
            item.Property(i => i.QuantityOnHand).HasPrecision(18, 3);

            item.HasMany(i => i.Movements)
                .WithOne(m => m.InventoryItem!)
                .HasForeignKey(m => m.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            item.Navigation(i => i.Movements).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<StockMovement>(movement =>
        {
            movement.HasKey(m => m.Id);
            movement.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            movement.Property(m => m.Quantity).HasPrecision(18, 3);
            movement.Property(m => m.Timestamp).IsRequired();
            movement.Property(m => m.Reason).HasMaxLength(500);
            movement.Property(m => m.WasteReason).HasConversion<string>().HasMaxLength(20);
            movement.Property(m => m.Note).HasMaxLength(1000);

            movement.HasIndex(m => new { m.InventoryItemId, m.Timestamp });
        });
    }
}
