using Microsoft.AspNetCore.Mvc;
using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.Commands.PureCreateProduct;
using WarehouseApi.Features.Products.Commands.PureDeleteProduct;
using WarehouseApi.Features.Products.Commands.PureUpdateStock;
using WarehouseApi.Features.Products.DTOs;
using WarehouseApi.Features.Products.Queries.PureGetProductById;
using WarehouseApi.Features.Products.Queries.PureGetProductsList;

namespace WarehouseApi.Controllers;

[ApiController, Route("api/pure-cqrs-demo")]
public class PureCqrsDemoController(
    IQueryHandler<PureGetProductsListQuery, IReadOnlyList<ProductDto>> list,
    IQueryHandler<PureGetProductByIdQuery, ProductDto?> byId,
    ICommandHandler<PureCreateProductCommand, ProductDto> create,
    ICommandHandler<PureUpdateStockCommand, ProductDto> updateStock,
    ICommandHandler<PureDeleteProductCommand, bool> delete) : ControllerBase
{
    /// <summary>Список товарів: пошук за назвою/SKU, категорія та onlyInStock=true.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetList(string? searchTerm, string? category, bool? onlyInStock, CancellationToken cancellationToken) =>
        Ok(await list.HandleAsync(new(searchTerm, category, onlyInStock), cancellationToken));

    /// <summary>Товар за ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(200), ProducesResponseType(404)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await byId.HandleAsync(new(id), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Створення товару. SKU унікальний, початковий залишок — від нуля.</summary>
    [HttpPost]
    [ProducesResponseType<ProductDto>(201), ProducesResponseType(400), ProducesResponseType(409)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await create.HandleAsync(new(request.Name, request.Sku, request.Category, request.Description,
            request.Price, request.StockQuantity), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Ручна зміна залишку з причиною. Від’ємний кінцевий залишок заборонений.</summary>
    [HttpPatch("{id:int}/stock")]
    [ProducesResponseType<ProductDto>(200), ProducesResponseType(400), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<ActionResult<ProductDto>> UpdateStock(int id, UpdateStockRequest request, CancellationToken cancellationToken) =>
        Ok(await updateStock.HandleAsync(new(id, request.QuantityChange, request.Reason), cancellationToken));

    /// <summary>Видалення товару та його пов’язаної історії.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204), ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await delete.HandleAsync(new(id), cancellationToken) ? NoContent() : NotFound();
}
