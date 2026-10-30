using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;
using WarehouseApi.Features.Suppliers.Commands.CreateSupplier;
using WarehouseApi.Features.Suppliers.Commands.RestockProductFromSupplier;
using WarehouseApi.Features.Suppliers.DTOs;
using WarehouseApi.Features.Suppliers.Queries.GetSupplierById;
using WarehouseApi.Features.Suppliers.Queries.GetSuppliersList;

namespace WarehouseApi.Controllers;

[ApiController, Route("api/suppliers")]
public class SuppliersController(
    IQueryHandler<GetSuppliersListQuery, IReadOnlyList<SupplierDto>> list,
    IQueryHandler<GetSupplierByIdQuery, SupplierDto?> byId,
    ICommandHandler<CreateSupplierCommand, SupplierDto> create,
    ICommandHandler<RestockProductFromSupplierCommand, ProductDto> restock) : ControllerBase
{
    /// <summary>Пошук за компанією/ЄДРПОУ. Типово показуються лише активні; onlyActive=false — усі.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetList(CancellationToken cancellationToken,
        string? searchTerm = null, bool? onlyActive = true) => Ok(await list.HandleAsync(new(searchTerm, onlyActive), cancellationToken));

    /// <summary>Деталі постачальника за ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SupplierDto>(200), ProducesResponseType(404)]
    public async Task<ActionResult<SupplierDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var supplier = await byId.HandleAsync(new(id), cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    /// <summary>Створення постачальника з унікальним кодом ЄДРПОУ/ІПН.</summary>
    [HttpPost]
    [ProducesResponseType<SupplierDto>(201), ProducesResponseType(400), ProducesResponseType(409)]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await create.HandleAsync(new(request.CompanyName, request.EdrpouCode,
            request.ContactPerson, request.Phone, request.Email, request.Address), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    /// <summary>Поставка: збільшення залишку та атомарне додавання історії з постачальником і накладною.</summary>
    [HttpPost("restock")]
    [ProducesResponseType<ProductDto>(200), ProducesResponseType(400), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<ActionResult<ProductDto>> Restock(RestockProductRequest request, CancellationToken cancellationToken) =>
        Ok(await restock.HandleAsync(new(request.SupplierId, request.ProductId, request.Quantity,
            request.PurchasePrice, request.InvoiceNumber), cancellationToken));
}
