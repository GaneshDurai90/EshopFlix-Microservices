using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record UpdateQuantityCommand(long CartId, int ItemId, int DeltaQuantity) : ICommand<int>;
}