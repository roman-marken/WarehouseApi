using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand(string CompanyName, string EdrpouCode, string? ContactPerson,
    string? Phone, string? Email, string? Address) : ICommand<SupplierDto>;
