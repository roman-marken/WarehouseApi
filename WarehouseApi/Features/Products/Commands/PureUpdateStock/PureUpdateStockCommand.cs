using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Commands.PureUpdateStock;

public record PureUpdateStockCommand(int ProductId, int QuantityChange, string Reason) : ICommand<ProductDto>;
