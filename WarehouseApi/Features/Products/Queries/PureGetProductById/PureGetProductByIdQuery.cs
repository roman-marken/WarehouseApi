using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Queries.PureGetProductById;

public record PureGetProductByIdQuery(int Id) : IQuery<ProductDto?>;
