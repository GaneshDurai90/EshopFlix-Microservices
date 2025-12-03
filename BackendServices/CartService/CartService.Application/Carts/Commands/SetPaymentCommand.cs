using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record SetPaymentCommand(long CartId, string Method, decimal AmountAuthorized, string CurrencyCode, string Status) : ICommand<bool>;
}