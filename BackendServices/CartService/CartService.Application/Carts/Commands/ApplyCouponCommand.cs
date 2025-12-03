using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record ApplyCouponCommand(long CartId, string Code, decimal Amount, string? Description) : ICommand<bool>;
}