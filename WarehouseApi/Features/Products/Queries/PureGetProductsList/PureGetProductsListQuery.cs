using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Queries.PureGetProductsList;

public record PureGetProductsListQuery(string? SearchTerm = null, string? Category = null, bool? OnlyInStock = null)
    : IQuery<IReadOnlyList<ProductDto>>;
