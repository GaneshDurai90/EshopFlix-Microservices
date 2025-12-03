using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record MakeInActiveCommand(long CartId) : ICommand<bool>;
}