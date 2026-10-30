using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarehouseApi.Data;

public class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseDbContext>
{
    public WarehouseDbContext CreateDbContext(string[] args)
    {
        string path = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(path, "WarehouseApi.csproj")) && File.Exists(Path.Combine(path, "WarehouseApi", "WarehouseApi.csproj")))
            path = Path.Combine(path, "WarehouseApi");
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlite($"Data Source={Path.Combine(path, "warehouse.db")}").Options;
        return new WarehouseDbContext(options);
    }
}
