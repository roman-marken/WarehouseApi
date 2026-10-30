using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;
using WarehouseApi.Domain.Entities;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Commands.PureCreateProduct;

public class PureCreateProductCommandHandler(WarehouseDbContext context) : ICommandHandler<PureCreateProductCommand, ProductDto>
{
    public async Task<ProductDto> HandleAsync(PureCreateProductCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Price <= 0 || command.StockQuantity < 0 || string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 3)
            throw new InvalidOperationException("Перевірте назву, ціну та початковий залишок товару.");
        string sku = command.Sku.Trim().ToUpperInvariant();
        if (await context.Products.AnyAsync(product => product.Sku == sku, cancellationToken))
            throw new ConflictException("Товар із таким SKU вже існує.");
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = command.Name.Trim(), Sku = sku, Category = command.Category.Trim(), Description = command.Description?.Trim(),
            Price = Math.Round(command.Price, 2), StockQuantity = command.StockQuantity, CreatedAt = now
        };
        context.Products.Add(product);
        if (product.StockQuantity > 0)
            context.StockTransactions.Add(new StockTransaction
            {
                Product = product, QuantityChange = product.StockQuantity, Reason = "Початковий залишок", Timestamp = now
            });
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        { throw new ConflictException("Товар із таким SKU вже існує."); }
        return product.ToDto();
    }
}
