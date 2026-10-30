using WarehouseApi.Common.Cqrs;
using WarehouseApi.Data;

namespace WarehouseApi.Features.Products.Commands.PureDeleteProduct;

public class PureDeleteProductCommandHandler(WarehouseDbContext context) : ICommandHandler<PureDeleteProductCommand, bool>
{
    public async Task<bool> HandleAsync(PureDeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([command.Id], cancellationToken);
        if (product is null) return false;
        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
