using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;
using WarehouseApi.Domain.Entities;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Suppliers.Commands.RestockProductFromSupplier;

public class RestockProductFromSupplierCommandHandler(WarehouseDbContext context)
    : ICommandHandler<RestockProductFromSupplierCommand, ProductDto>
{
    public async Task<ProductDto> HandleAsync(RestockProductFromSupplierCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity <= 0 || command.PurchasePrice <= 0 || string.IsNullOrWhiteSpace(command.InvoiceNumber))
            throw new InvalidOperationException("Кількість і закупівельна ціна мають бути додатними. Вкажіть номер накладної.");
        var supplier = await context.Suppliers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == command.SupplierId, cancellationToken)
            ?? throw new NotFoundException("Постачальника не знайдено.");
        if (!supplier.IsActive) throw new InvalidOperationException("Поставки від неактивного постачальника заборонені.");
        var product = await context.Products.FindAsync([command.ProductId], cancellationToken)
            ?? throw new NotFoundException("Товар не знайдено.");
        long finalStock = (long)product.StockQuantity + command.Quantity;
        if (finalStock > int.MaxValue) throw new InvalidOperationException("Залишок перевищує допустиме значення.");

        var now = DateTime.UtcNow;
        product.StockQuantity = (int)finalStock;
        product.UpdatedAt = now;
        product.Version = Guid.NewGuid();
        context.StockTransactions.Add(new StockTransaction
        {
            ProductId = product.Id, QuantityChange = command.Quantity, Timestamp = now,
            Reason = string.Create(CultureInfo.GetCultureInfo("uk-UA"),
                $"Поставка від '{supplier.CompanyName}' за накладною №{command.InvoiceNumber.Trim()} (ціна закупки: {command.PurchasePrice:C})")
        });
        // Один SaveChanges атомарно зберігає і новий залишок, і запис історії.
        await context.SaveChangesAsync(cancellationToken);
        return product.ToDto();
    }
}
