using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace WarehouseApi.Tests;

public class WarehouseFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "WarehouseApi.Tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_directory);
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Warehouse"] = $"Data Source={Path.Combine(_directory, "warehouse.db")}",
            ["Logging:LogLevel:Default"] = "Warning"
        }));
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_directory))
        {
            SqliteConnection.ClearAllPools();
            string path = Path.GetFullPath(_directory);
            string allowed = Path.Combine(Path.GetTempPath(), "WarehouseApi.Tests") + Path.DirectorySeparatorChar;
            if (!path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected test path.");
            Directory.Delete(path, recursive: true);
        }
    }
}
