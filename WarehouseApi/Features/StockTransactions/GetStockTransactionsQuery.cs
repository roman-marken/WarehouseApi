using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;

namespace WarehouseApi.Features.StockTransactions;

public record StockTransactionDto(int Id, int ProductId, int QuantityChange, string Reason, DateTime Timestamp);
public record GetStockTransactionsQuery(int ProductId) : IQuery<IReadOnlyList<StockTransactionDto>>;

public class GetStockTransactionsQueryHandler(WarehouseDbContext context)
    : IQueryHandler<GetStockTransactionsQuery, IReadOnlyList<StockTransactionDto>>
{
    public async Task<IReadOnlyList<StockTransactionDto>> HandleAsync(GetStockTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await context.Products.AnyAsync(product => product.Id == query.ProductId, cancellationToken))
            throw new NotFoundException("Товар не знайдено.");
        return await context.StockTransactions.AsNoTracking().Where(transaction => transaction.ProductId == query.ProductId)
            .OrderByDescending(transaction => transaction.Timestamp).ThenByDescending(transaction => transaction.Id)
            .Select(transaction => new StockTransactionDto(transaction.Id, transaction.ProductId, transaction.QuantityChange, transaction.Reason, transaction.Timestamp))
            .ToListAsync(cancellationToken);
    }
}
