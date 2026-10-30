using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Common.Exceptions;
using WarehouseApi.Data;
using WarehouseApi.Domain.Entities;
using WarehouseApi.Features.Suppliers.DTOs;

namespace WarehouseApi.Features.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler(WarehouseDbContext context) : ICommandHandler<CreateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> HandleAsync(CreateSupplierCommand command, CancellationToken cancellationToken = default)
    {
        string code = command.EdrpouCode.Trim();
        if (string.IsNullOrWhiteSpace(command.CompanyName) || code.Length is < 8 or > 10 || code.Any(character => character is < '0' or > '9'))
            throw new InvalidOperationException("Перевірте назву компанії та код ЄДРПОУ/ІПН.");
        if (await context.Suppliers.AnyAsync(supplier => supplier.EdrpouCode == code, cancellationToken))
            throw new ConflictException("Постачальник із таким кодом ЄДРПОУ/ІПН уже існує.");
        var supplier = new Supplier
        {
            CompanyName = command.CompanyName.Trim(), EdrpouCode = code,
            ContactPerson = command.ContactPerson?.Trim() ?? string.Empty, Phone = command.Phone?.Trim() ?? string.Empty,
            Email = command.Email?.Trim() ?? string.Empty, Address = command.Address?.Trim() ?? string.Empty,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        context.Suppliers.Add(supplier);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        { throw new ConflictException("Постачальник із таким кодом ЄДРПОУ/ІПН уже існує."); }
        return supplier.ToDto();
    }
}
