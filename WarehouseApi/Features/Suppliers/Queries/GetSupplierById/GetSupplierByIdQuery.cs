using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(int Id) : IQuery<SupplierDto?>;
