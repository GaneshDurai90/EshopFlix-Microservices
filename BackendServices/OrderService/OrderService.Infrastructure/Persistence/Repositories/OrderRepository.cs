using OrderService.Application.Repositories;
using OrderService.Domain.Entities;


namespace OrderService.Infrastructure.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        OrderServiceDbContext _db;
        public OrderRepository(OrderServiceDbContext db)
        {
            _db = db;
        }

        public List<Order> GetAllOrder()
        {
            return _db.Orders.ToList();
        }
        public void SaveOrder(Order order, long cartId)
        {
            try
            {
                _db.Orders.Add(order);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<bool> DeleteOrder(Guid orderId)
        {
            try
            {
                Order order = _db.Orders.Where(x => x.OrderId == orderId).FirstOrDefault();

                if (order != null)
                {
                    _db.Remove(order);
                    await _db.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> AcceptedOrder(Guid OrderId, DateTime AcceptedDateTime)
        {
            try
            {
                var order = _db.Orders.Where(x => x.OrderId == OrderId).FirstOrDefault();
                if (order != null)
                {
                    order.AcceptDate = AcceptedDateTime;
                    await _db.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

            }
            return false;
        }
        public Order GetOrder(Guid orderId)
        {
            return _db.Orders.Where(x => x.OrderId == orderId).FirstOrDefault();
        }
    }
}
