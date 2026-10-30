using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Queries.GetSuppliersList;

public record GetSuppliersListQuery(string? SearchTerm = null, bool? OnlyActive = true) : IQuery<IReadOnlyList<SupplierDto>>;
