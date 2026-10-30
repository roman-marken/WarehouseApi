using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WarehouseApi.Data;
using WarehouseApi.Domain.Entities;

namespace WarehouseApi.Tests;

public class MigrationTests
{
    [Fact]
    public async Task AddSuppliersTable_PreservesProductsAndEnforcesUniqueCode()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WarehouseDbContext>().UseSqlite(connection).Options;
        await using var db = new WarehouseDbContext(options);
        await db.Database.GetService<IMigrator>().MigrateAsync("InitialWarehouse");
        var product = new Product { Name = "Збережений товар", Sku = "OLD-1", Category = "Тест", Price = 100m, StockQuantity = 10, CreatedAt = DateTime.UtcNow };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        await db.Database.MigrateAsync();

        Assert.Equal(10, (await db.Products.SingleAsync(item => item.Id == product.Id)).StockQuantity);
        db.Suppliers.Add(new Supplier { CompanyName = "Компанія", EdrpouCode = "41234567", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        db.Suppliers.Add(new Supplier { CompanyName = "Інша компанія", EdrpouCode = "41234567", CreatedAt = DateTime.UtcNow });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(2067, Assert.IsType<SqliteException>(exception.InnerException).SqliteExtendedErrorCode);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
