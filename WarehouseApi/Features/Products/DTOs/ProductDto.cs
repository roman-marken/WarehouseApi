using System.Linq.Expressions;
using WarehouseApi.Domain.Entities;

namespace WarehouseApi.Features.Products.DTOs;

public record ProductDto(int Id, string Name, string Sku, string Category, string? Description,
    decimal Price, int StockQuantity, DateTime CreatedAt, DateTime? UpdatedAt);

public static class ProductMapping
{
    public static readonly Expression<Func<Product, ProductDto>> Projection = product => new(
        product.Id, product.Name, product.Sku, product.Category, product.Description,
        product.Price, product.StockQuantity, product.CreatedAt, product.UpdatedAt);

    public static ProductDto ToDto(this Product product) => new(product.Id, product.Name, product.Sku,
        product.Category, product.Description, product.Price, product.StockQuantity, product.CreatedAt, product.UpdatedAt);
}
