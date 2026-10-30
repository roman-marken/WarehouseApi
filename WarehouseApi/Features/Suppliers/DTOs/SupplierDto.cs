using System.Linq.Expressions;
using WarehouseApi.Domain.Entities;

namespace WarehouseApi.Features.Suppliers.DTOs;

public record SupplierDto(int Id, string CompanyName, string EdrpouCode, string ContactPerson,
    string Phone, string Email, string Address, bool IsActive, DateTime CreatedAt);

public static class SupplierMapping
{
    public static readonly Expression<Func<Supplier, SupplierDto>> Projection = supplier => new(
        supplier.Id, supplier.CompanyName, supplier.EdrpouCode, supplier.ContactPerson, supplier.Phone,
        supplier.Email, supplier.Address, supplier.IsActive, supplier.CreatedAt);

    public static SupplierDto ToDto(this Supplier supplier) => new(supplier.Id, supplier.CompanyName, supplier.EdrpouCode,
        supplier.ContactPerson, supplier.Phone, supplier.Email, supplier.Address, supplier.IsActive, supplier.CreatedAt);
}
