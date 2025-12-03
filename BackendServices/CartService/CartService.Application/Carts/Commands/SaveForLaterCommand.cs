using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record SaveForLaterCommand(long CartId, int ItemId) : ICommand<bool>;
}