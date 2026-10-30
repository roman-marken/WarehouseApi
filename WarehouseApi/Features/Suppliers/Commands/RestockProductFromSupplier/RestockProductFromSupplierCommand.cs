using WarehouseApi.Common.Cqrs;
using WarehouseApi.Features.Products.DTOs;

namespace WarehouseApi.Features.Suppliers.Commands.RestockProductFromSupplier;

public record RestockProductFromSupplierCommand(int SupplierId, int ProductId, int Quantity,
    decimal PurchasePrice, string InvoiceNumber) : ICommand<ProductDto>;
