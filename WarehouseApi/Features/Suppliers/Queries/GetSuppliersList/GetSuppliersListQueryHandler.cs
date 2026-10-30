using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Queries.GetSuppliersList;

public class GetSuppliersListQueryHandler(WarehouseDbContext context) : IQueryHandler<GetSuppliersListQuery, IReadOnlyList<SupplierDto>>
{
    public async Task<IReadOnlyList<SupplierDto>> HandleAsync(GetSuppliersListQuery query, CancellationToken cancellationToken = default)
    {
        var suppliers = context.Suppliers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim();
            suppliers = suppliers.Where(supplier => supplier.CompanyName.Contains(term) || supplier.EdrpouCode.Contains(term));
        }
        if (query.OnlyActive == true) suppliers = suppliers.Where(supplier => supplier.IsActive);
        return await suppliers.OrderByDescending(supplier => supplier.CreatedAt).ThenByDescending(supplier => supplier.Id)
            .Select(SupplierMapping.Projection).ToListAsync(cancellationToken);
    }
}
