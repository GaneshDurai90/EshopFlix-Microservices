
using CartService.Application.CQRS;
using CartService.Application.DTOs;
using CartService.Domain.Entities;

namespace CartService.Application.Carts.Commands
{
    public sealed record AddItemCommand(long UserId, CartItem Item) : ICommand<CartDTO>;
}
