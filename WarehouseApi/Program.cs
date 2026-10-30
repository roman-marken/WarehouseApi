using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;
using WarehouseApi.Features.Products.Commands.PureCreateProduct;
using WarehouseApi.Features.Products.Commands.PureDeleteProduct;
using WarehouseApi.Features.Products.Commands.PureUpdateStock;
using WarehouseApi.Features.Products.DTOs;
using WarehouseApi.Features.Products.Queries.PureGetProductById;
using WarehouseApi.Features.Products.Queries.PureGetProductsList;
using WarehouseApi.Features.StockTransactions;
using WarehouseApi.Features.Suppliers.Commands.CreateSupplier;
using WarehouseApi.Features.Suppliers.Commands.RestockProductFromSupplier;
using WarehouseApi.Features.Suppliers.DTOs;
using WarehouseApi.Features.Suppliers.Queries.GetSupplierById;
using WarehouseApi.Features.Suppliers.Queries.GetSuppliersList;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<WarehouseDbContext>(options =>
{
    var connection = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Warehouse") ?? "Data Source=warehouse.db");
    if (connection.DataSource != ":memory:" && !Path.IsPathRooted(connection.DataSource))
        connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);
    options.UseSqlite(connection.ConnectionString);
});

// Pure CQRS: жодного dispatcher/MediatR — кожен хендлер зареєстрований явно.
builder.Services.AddScoped<IQueryHandler<PureGetProductsListQuery, IReadOnlyList<ProductDto>>, PureGetProductsListQueryHandler>();
builder.Services.AddScoped<IQueryHandler<PureGetProductByIdQuery, ProductDto?>, PureGetProductByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<PureCreateProductCommand, ProductDto>, PureCreateProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PureUpdateStockCommand, ProductDto>, PureUpdateStockCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PureDeleteProductCommand, bool>, PureDeleteProductCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetSuppliersListQuery, IReadOnlyList<SupplierDto>>, GetSuppliersListQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetSupplierByIdQuery, SupplierDto?>, GetSupplierByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateSupplierCommand, SupplierDto>, CreateSupplierCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RestockProductFromSupplierCommand, ProductDto>, RestockProductFromSupplierCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetStockTransactionsQuery, IReadOnlyList<StockTransactionDto>>, GetStockTransactionsQueryHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("WarehouseApi — Pure CQRS"));
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}
app.MapControllers();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<WarehouseDbContext>().Database.MigrateAsync();
app.Run();

public partial class Program { }
