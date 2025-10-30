using OrderService.Application.DTOs;
using OrderService.Domain.Entities;


namespace OrderService.Application.Services.Abstractions
{
    public interface IOrderAppService
    {
        List<Order> GetAllOrder();
        void SaveOrder(OrderDTO order, long cartId);
        Order GetOrder(Guid orderId);
        Task<bool> AcceptedOrder(Guid OrderId, DateTime AcceptedDateTime);
        Task<bool> DeleteOrder(Guid orderId);
    }
}
