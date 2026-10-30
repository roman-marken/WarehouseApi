using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;
using WarehouseApi.Features.Products.Commands.PureUpdateStock;
using WarehouseApi.Features.Products.DTOs;
using WarehouseApi.Features.Products.Queries.PureGetProductsList;
using WarehouseApi.Features.StockTransactions;
using WarehouseApi.Features.Suppliers.DTOs;
using WarehouseApi.Features.Suppliers.Queries.GetSuppliersList;

namespace WarehouseApi.Tests;

public class WarehouseWorkflowTests : IDisposable
{
    private readonly WarehouseFactory _factory = new();
    private readonly HttpClient _client;
    public WarehouseWorkflowTests() => _client = _factory.Client();

    private async Task<ProductDto> ProductAsync(string name = "Ноутбук", string sku = "LAP-001", string category = "Електроніка", int stock = 10)
    {
        var response = await _client.PostAsJsonAsync("/api/pure-cqrs-demo", new CreateProductRequest
        {
            Name = name, Sku = sku, Category = category, Price = 1000m, StockQuantity = stock
        });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private async Task<SupplierDto> SupplierAsync(string company = "ТОВ Електропостач", string code = "41234567")
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest
        {
            CompanyName = company, EdrpouCode = code, ContactPerson = "Олена Коваль",
            Phone = "+380671234567", Email = "office@example.com", Address = "Київ, вул. Складська, 1"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SupplierDto>())!;
    }

    private Task<HttpResponseMessage> RestockAsync(int supplierId, int productId, int quantity = 25, decimal price = 125.50m, string invoice = "INV-2026/09-44") =>
        _client.PostAsJsonAsync("/api/suppliers/restock", new RestockProductRequest
        {
            SupplierId = supplierId, ProductId = productId, Quantity = quantity, PurchasePrice = price, InvoiceNumber = invoice
        });

    [Fact]
    public async Task FullWorkflow_TenPlusTwentyFiveCreatesHistoryAndSupportsStockUpdateAndDelete()
    {
        var product = await ProductAsync();
        var supplier = await SupplierAsync();
        Assert.True(supplier.IsActive);
        var response = await RestockAsync(supplier.Id, product.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var restocked = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(35, restocked.StockQuantity);
        Assert.Equal(1000m, restocked.Price); // Закупівельна ціна не підміняє продажну.
        Assert.Equal(DateTimeKind.Utc, restocked.UpdatedAt!.Value.Kind);
        var reread = (await _client.GetFromJsonAsync<ProductDto>($"/api/pure-cqrs-demo/{product.Id}"))!;
        Assert.Equal(35, reread.StockQuantity);
        var history = (await _client.GetFromJsonAsync<List<StockTransactionDto>>($"/api/stock-transactions?productId={product.Id}"))!;
        Assert.Equal(2, history.Count); // Початковий залишок та поставка.
        var transaction = Assert.Single(history, item => item.QuantityChange == 25);
        Assert.Contains(supplier.CompanyName, transaction.Reason);
        Assert.Contains("INV-2026/09-44", transaction.Reason);
        Assert.Contains("125,50", transaction.Reason);
        Assert.Equal(DateTimeKind.Utc, transaction.Timestamp.Kind);
        var changed = await _client.PatchAsJsonAsync($"/api/pure-cqrs-demo/{product.Id}/stock", new UpdateStockRequest
        {
            QuantityChange = -5, Reason = "Відвантаження клієнту"
        });
        Assert.Equal(30, (await changed.Content.ReadFromJsonAsync<ProductDto>())!.StockQuantity);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/pure-cqrs-demo/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/pure-cqrs-demo/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/pure-cqrs-demo/{product.Id}")).StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<WarehouseDbContext>().StockTransactions.ToListAsync());
    }

    [Fact]
    public async Task ProductQueries_FilterByNameSkuCategoryAndStockAndSortNewestFirst()
    {
        var laptop = await ProductAsync();
        var mouse = await ProductAsync("Миша", "MOUSE-001", stock: 0);
        var chair = await ProductAsync("Стілець", "CHAIR-001", "Меблі", 5);
        var all = (await _client.GetFromJsonAsync<List<ProductDto>>("/api/pure-cqrs-demo"))!;
        Assert.Equal(new[] { chair.Id, mouse.Id, laptop.Id }, all.Select(item => item.Id));
        var sku = (await _client.GetFromJsonAsync<List<ProductDto>>("/api/pure-cqrs-demo?searchTerm=lap"))!;
        Assert.Equal(laptop.Id, Assert.Single(sku).Id);
        var byName = (await _client.GetFromJsonAsync<List<ProductDto>>("/api/pure-cqrs-demo?searchTerm=" + Uri.EscapeDataString("Миша")))!;
        Assert.Equal(mouse.Id, Assert.Single(byName).Id);
        var available = (await _client.GetFromJsonAsync<List<ProductDto>>("/api/pure-cqrs-demo?onlyInStock=true&category=" + Uri.EscapeDataString("Електроніка")))!;
        Assert.Equal(laptop.Id, Assert.Single(available).Id);
        Assert.Equal(3, (await _client.GetFromJsonAsync<List<ProductDto>>("/api/pure-cqrs-demo?onlyInStock=false"))!.Count);
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(0)]
    public async Task InvalidStockChange_DoesNotChangeProductOrHistory(int quantity)
    {
        var product = await ProductAsync();
        var response = await _client.PatchAsJsonAsync($"/api/pure-cqrs-demo/{product.Id}/stock", new UpdateStockRequest { QuantityChange = quantity, Reason = "Перевірка" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(10, (await _client.GetFromJsonAsync<ProductDto>($"/api/pure-cqrs-demo/{product.Id}"))!.StockQuantity);
        Assert.Single((await _client.GetFromJsonAsync<List<StockTransactionDto>>($"/api/stock-transactions?productId={product.Id}"))!);
    }

    [Fact]
    public async Task StockCanBeReducedExactlyToZero()
    {
        var product = await ProductAsync();
        var response = await _client.PatchAsJsonAsync($"/api/pure-cqrs-demo/{product.Id}/stock", new { quantityChange = -10, reason = "Повне відвантаження" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<ProductDto>())!.StockQuantity);
    }

    [Fact]
    public async Task MissingProductsAndSuppliers_Return404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/pure-cqrs-demo/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/suppliers/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsJsonAsync("/api/pure-cqrs-demo/9999/stock", new { quantityChange = 1, reason = "Перевірка" })).StatusCode);
        var product = await ProductAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await RestockAsync(9999, product.Id)).StatusCode);
        var supplier = await SupplierAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await RestockAsync(supplier.Id, 9999)).StatusCode);
    }

    [Fact]
    public async Task DuplicateSupplierCodeAndCaseInsensitiveSku_Return409()
    {
        await SupplierAsync();
        var duplicate = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { CompanyName = "Інша компанія", EdrpouCode = "41234567" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        await ProductAsync(sku: "lap-001");
        var duplicateSku = await _client.PostAsJsonAsync("/api/pure-cqrs-demo", new CreateProductRequest
        {
            Name = "Інший ноутбук", Sku = "LAP-001", Category = "Електроніка", Price = 100m
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateSku.StatusCode);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("12345678901")]
    [InlineData("abcd1234")]
    [InlineData("")]
    public async Task InvalidSupplierCode_Returns400(string code)
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { CompanyName = "Компанія", EdrpouCode = code });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LeadingZeroCode_IsPreservedAndPublicFieldsRoundTrip()
    {
        var supplier = await SupplierAsync(code: "00123456");
        var reread = (await _client.GetFromJsonAsync<SupplierDto>($"/api/suppliers/{supplier.Id}"))!;
        Assert.Equal("00123456", reread.EdrpouCode);
        Assert.Equal("Олена Коваль", reread.ContactPerson);
        Assert.Equal("office@example.com", reread.Email);
        Assert.Equal(DateTimeKind.Utc, reread.CreatedAt.Kind);
    }

    [Fact]
    public async Task SupplierQueries_SearchAndFilterActiveByDefault()
    {
        var active = await SupplierAsync();
        var inactive = await SupplierAsync("ТОВ Меблепостач", "51234567");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            (await db.Suppliers.FindAsync(inactive.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(active.Id, Assert.Single((await _client.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers"))!).Id);
        Assert.Equal(2, (await _client.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers?onlyActive=false"))!.Count);
        var byCode = (await _client.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers?searchTerm=4123"))!;
        Assert.Equal(active.Id, Assert.Single(byCode).Id);
        var byCompany = (await _client.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers?searchTerm=" + Uri.EscapeDataString("Електропостач")))!;
        Assert.Equal(active.Id, Assert.Single(byCompany).Id);
    }

    [Fact]
    public async Task InactiveSupplier_RestockIsRejectedWithoutSideEffects()
    {
        var product = await ProductAsync();
        var supplier = await SupplierAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
            (await db.Suppliers.FindAsync(supplier.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await RestockAsync(supplier.Id, product.Id)).StatusCode);
        Assert.Equal(10, (await _client.GetFromJsonAsync<ProductDto>($"/api/pure-cqrs-demo/{product.Id}"))!.StockQuantity);
        Assert.Single((await _client.GetFromJsonAsync<List<StockTransactionDto>>($"/api/stock-transactions?productId={product.Id}"))!);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(25, 0)]
    [InlineData(25, -1)]
    [InlineData(int.MaxValue, 100)]
    public async Task InvalidRestockQuantityPriceOrOverflow_Returns400(int quantity, int price)
    {
        var product = await ProductAsync();
        var supplier = await SupplierAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await RestockAsync(supplier.Id, product.Id, quantity, price)).StatusCode);
        Assert.Equal(10, (await _client.GetFromJsonAsync<ProductDto>($"/api/pure-cqrs-demo/{product.Id}"))!.StockQuantity);
    }

    [Fact]
    public async Task Restock_WithoutInvoice_Returns400()
    {
        var product = await ProductAsync();
        var supplier = await SupplierAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await RestockAsync(supplier.Id, product.Id, invoice: " ")).StatusCode);
    }

    [Fact]
    public async Task QueryHandlers_DoNotTrackEntitiesOrMutateState()
    {
        await ProductAsync();
        await SupplierAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var products = scope.ServiceProvider.GetRequiredService<IQueryHandler<PureGetProductsListQuery, IReadOnlyList<ProductDto>>>();
        var suppliers = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSuppliersListQuery, IReadOnlyList<SupplierDto>>>();
        Assert.Single(await products.HandleAsync(new()));
        Assert.Single(await suppliers.HandleAsync(new()));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task FailedHistoryInsert_RollsBackProductUpdate()
    {
        var product = await ProductAsync();
        var supplier = await SupplierAsync();
        using (var scope = _factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<WarehouseDbContext>().Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RejectRestock BEFORE INSERT ON StockTransactions WHEN NEW.Reason LIKE 'Поставка%' BEGIN SELECT RAISE(ABORT, 'Test failure'); END;");
        Assert.Equal(HttpStatusCode.InternalServerError, (await RestockAsync(supplier.Id, product.Id)).StatusCode);
        using var checkScope = _factory.Services.CreateScope();
        var check = checkScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        Assert.Equal(10, (await check.Products.AsNoTracking().SingleAsync(item => item.Id == product.Id)).StockQuantity);
        Assert.Equal(1, await check.StockTransactions.CountAsync());
    }

    [Fact]
    public async Task StaleCommand_CannotOverwriteAnotherStockChangeOrLeaveOrphanHistory()
    {
        var product = await ProductAsync();
        using var firstScope = _factory.Services.CreateScope();
        using var secondScope = _factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        await first.Products.FindAsync(product.Id);
        await second.Products.FindAsync(product.Id);
        await new PureUpdateStockCommandHandler(first).HandleAsync(new(product.Id, 5, "Перша зміна"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => new PureUpdateStockCommandHandler(second).HandleAsync(new(product.Id, 8, "Застаріла зміна")));
        using var checkScope = _factory.Services.CreateScope();
        var check = checkScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        Assert.Equal(15, (await check.Products.AsNoTracking().SingleAsync(item => item.Id == product.Id)).StockQuantity);
        Assert.Equal(2, await check.StockTransactions.CountAsync());
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}
