using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record RemoveCouponCommand(long CartId, string Code) : ICommand<bool>;
}