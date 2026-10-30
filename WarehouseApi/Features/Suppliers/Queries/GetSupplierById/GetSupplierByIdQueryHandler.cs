using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdQueryHandler(WarehouseDbContext context) : IQueryHandler<GetSupplierByIdQuery, SupplierDto?>
{
    public Task<SupplierDto?> HandleAsync(GetSupplierByIdQuery query, CancellationToken cancellationToken = default) =>
        context.Suppliers.AsNoTracking().Where(supplier => supplier.Id == query.Id).Select(SupplierMapping.Projection)
            .SingleOrDefaultAsync(cancellationToken);
}
