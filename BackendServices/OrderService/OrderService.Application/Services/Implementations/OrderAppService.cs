using OrderService.Application.DTOs;
using OrderService.Application.Repositories;
using OrderService.Application.Services.Abstractions;
using OrderService.Domain.Entities;


namespace OrderService.Application.Services.Implementations
{
    public class OrderAppService : IOrderAppService
    {
        IOrderRepository _orderRepository;
        public OrderAppService(IOrderRepository repository)
        {
            _orderRepository = repository;
        }
        public Task<bool> AcceptedOrder(Guid OrderId, DateTime AcceptedDateTime)
        {
            return _orderRepository.AcceptedOrder(OrderId, AcceptedDateTime);
        }

        public Task<bool> DeleteOrder(Guid orderId)
        {
            return _orderRepository.DeleteOrder(orderId);
        }

        public List<Order> GetAllOrder()
        {
            return _orderRepository.GetAllOrder();
        }

        public Order GetOrder(Guid orderId)
        {
            return _orderRepository.GetOrder(orderId);
        }

        public void SaveOrder(OrderDTO model, long cartId)
        {
            Order order = new Order
            {
                PaymentId = model.PaymentId,
                OrderId = model.OrderId,
                CreatedDate = model.CreatedDate,
                UserId = model.UserId
            };

            _orderRepository.SaveOrder(order, cartId);
        }
    }
}
