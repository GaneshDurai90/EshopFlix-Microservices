using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderService.Domain.Entities;


namespace OrderService.Application.Repositories
{
    public interface IOrderRepository
    {
        List<Order> GetAllOrder();
        void SaveOrder(Order order, long cartId);
        Order GetOrder(Guid orderId);
        Task<bool> AcceptedOrder(Guid OrderId, DateTime AcceptedDateTime);
        Task<bool> DeleteOrder(Guid orderId);
    }
}
