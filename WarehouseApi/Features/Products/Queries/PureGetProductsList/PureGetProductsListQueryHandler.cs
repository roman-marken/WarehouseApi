using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Products.Queries.PureGetProductsList;

public class PureGetProductsListQueryHandler(WarehouseDbContext context)
    : IQueryHandler<PureGetProductsListQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> HandleAsync(PureGetProductsListQuery query, CancellationToken cancellationToken = default)
    {
        var products = context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim();
            string sku = term.ToUpperInvariant();
            products = products.Where(product => product.Name.Contains(term) || product.Sku.Contains(sku));
        }
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            string category = query.Category.Trim();
            products = products.Where(product => product.Category == category);
        }
        if (query.OnlyInStock == true) products = products.Where(product => product.StockQuantity > 0);
        return await products.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id)
            .Select(ProductMapping.Projection).ToListAsync(cancellationToken);
    }
}
