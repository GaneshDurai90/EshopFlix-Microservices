using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record DeleteItemCommand(long CartId, int ItemId) : ICommand<int>;
}