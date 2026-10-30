using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Queries.PureGetProductById;

public class PureGetProductByIdQueryHandler(WarehouseDbContext context) : IQueryHandler<PureGetProductByIdQuery, ProductDto?>
{
    public Task<ProductDto?> HandleAsync(PureGetProductByIdQuery query, CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().Where(product => product.Id == query.Id).Select(ProductMapping.Projection)
            .SingleOrDefaultAsync(cancellationToken);
}
