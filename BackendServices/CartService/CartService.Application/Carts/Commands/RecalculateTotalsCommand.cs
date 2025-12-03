using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record RecalculateTotalsCommand(long CartId) : ICommand<bool>;
}