using WarehouseApi.Common.Cqrs;

namespace WarehouseApi.Features.Products.Commands.PureDeleteProduct;

public record PureDeleteProductCommand(int Id) : ICommand<bool>;
