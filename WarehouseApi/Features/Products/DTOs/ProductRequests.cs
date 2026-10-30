using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Features.Products.DTOs;

public class CreateProductRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Sku { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
}

public class UpdateStockRequest
{
    public int QuantityChange { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
}
