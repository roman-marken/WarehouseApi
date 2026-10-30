using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Commands.PureCreateProduct;

public record PureCreateProductCommand(string Name, string Sku, string Category, string? Description,
    decimal Price, int StockQuantity) : ICommand<ProductDto>;
