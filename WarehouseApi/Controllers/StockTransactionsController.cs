using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.StockTransactions;

namespace WarehouseApi.Controllers;

[ApiController, Route("api/stock-transactions")]
public class StockTransactionsController(IQueryHandler<GetStockTransactionsQuery, IReadOnlyList<StockTransactionDto>> history) : ControllerBase
{
    /// <summary>Історія руху товару: додатковий read-only endpoint для перевірки поставок у Scalar.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StockTransactionDto>>(200), ProducesResponseType(400), ProducesResponseType(404)]
    public async Task<ActionResult<IReadOnlyList<StockTransactionDto>>> Get([Range(1, int.MaxValue)] int productId, CancellationToken cancellationToken) =>
        Ok(await history.HandleAsync(new(productId), cancellationToken));
}
