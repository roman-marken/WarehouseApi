using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Features.Suppliers.DTOs;

public class CreateSupplierRequest
{
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{8,10}$", ErrorMessage = "Код ЄДРПОУ/ІПН має містити від 8 до 10 цифр.")]
    public string EdrpouCode { get; set; } = string.Empty;
    [StringLength(100)] public string? ContactPerson { get; set; }
    [Phone, StringLength(20)] public string? Phone { get; set; }
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}

public class RestockProductRequest
{
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(typeof(decimal), "0.01", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PurchasePrice { get; set; }
    [Required, StringLength(100)] public string InvoiceNumber { get; set; } = string.Empty;
}
