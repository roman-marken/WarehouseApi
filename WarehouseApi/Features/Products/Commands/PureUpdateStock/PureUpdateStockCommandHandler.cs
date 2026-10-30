using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;
using WarehouseApi.Domain.Entities;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Commands.PureUpdateStock;

public class PureUpdateStockCommandHandler(WarehouseDbContext context) : ICommandHandler<PureUpdateStockCommand, ProductDto>
{
    public async Task<ProductDto> HandleAsync(PureUpdateStockCommand command, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([command.ProductId], cancellationToken)
            ?? throw new NotFoundException("Товар не знайдено.");
        if (command.QuantityChange == 0 || string.IsNullOrWhiteSpace(command.Reason))
            throw new InvalidOperationException("Вкажіть ненульову зміну залишку та причину.");
        long finalStock = (long)product.StockQuantity + command.QuantityChange;
        if (finalStock < 0) throw new InvalidOperationException("Недостатньо товару на складі. Залишок не може бути від’ємним.");
        if (finalStock > int.MaxValue) throw new InvalidOperationException("Залишок перевищує допустиме значення.");
        var now = DateTime.UtcNow;
        product.StockQuantity = (int)finalStock;
        product.UpdatedAt = now;
        product.Version = Guid.NewGuid();
        context.StockTransactions.Add(new StockTransaction
        {
            ProductId = product.Id, QuantityChange = command.QuantityChange, Reason = command.Reason.Trim(), Timestamp = now
        });
        await context.SaveChangesAsync(cancellationToken);
        return product.ToDto();
    }
}
