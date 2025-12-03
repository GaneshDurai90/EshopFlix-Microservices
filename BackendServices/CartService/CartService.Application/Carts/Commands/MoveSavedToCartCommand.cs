using CartService.Application.CQRS;

namespace CartService.Application.Carts.Commands
{
    public sealed record MoveSavedToCartCommand(int SavedItemId) : ICommand<bool>;
}