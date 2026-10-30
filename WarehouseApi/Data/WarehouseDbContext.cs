using Microsoft.EntityFrameworkCore;
using WarehouseApi.Domain.Entities;

namespace WarehouseApi.Data;

public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Product>(product =>
        {
            product.Property(item => item.Name).IsRequired().HasMaxLength(200);
            product.Property(item => item.Sku).IsRequired().HasMaxLength(50);
            product.HasIndex(item => item.Sku).IsUnique();
            product.Property(item => item.Category).IsRequired().HasMaxLength(100);
            product.Property(item => item.Description).HasMaxLength(1000);
            product.Property(item => item.Price).HasPrecision(18, 2);
            product.Property(item => item.Version).IsConcurrencyToken();
            product.Property(item => item.CreatedAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            product.Property(item => item.UpdatedAt).HasConversion(value => value,
                value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
            product.ToTable(table => table.HasCheckConstraint("CK_Products_NonNegativeStock", "\"StockQuantity\" >= 0"));
        });
        builder.Entity<StockTransaction>(transaction =>
        {
            transaction.Property(item => item.Reason).IsRequired().HasMaxLength(1000);
            transaction.Property(item => item.Timestamp).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            transaction.HasOne(item => item.Product).WithMany(product => product.StockTransactions)
                .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            transaction.HasIndex(item => new { item.ProductId, item.Timestamp });
        });
        builder.Entity<Supplier>(supplier =>
        {
            supplier.Property(item => item.CompanyName).IsRequired().HasMaxLength(200);
            supplier.Property(item => item.EdrpouCode).IsRequired().HasMaxLength(10);
            supplier.HasIndex(item => item.EdrpouCode).IsUnique();
            supplier.Property(item => item.ContactPerson).HasMaxLength(100);
            supplier.Property(item => item.Phone).HasMaxLength(20);
            supplier.Property(item => item.Email).HasMaxLength(100);
            supplier.Property(item => item.Address).HasMaxLength(500);
            supplier.Property(item => item.IsActive).HasDefaultValue(true);
            supplier.Property(item => item.CreatedAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        });
    }
}
