using StockService.Application.Repositories;
using StockService.Domain.Entities;


namespace StockService.Infrastructure.Persistence.Repositories
{
    public class StockRepository : IStockRepository
    {
        StockServiceDbContext _db;
        public StockRepository(StockServiceDbContext db)
        {
            _db = db;
        }
        public bool CheckStockAvailibility(int productId, int quantity)
        {
            return _db.Stocks.Any(x => x.ProductId == productId && x.Quantity >= quantity);
        }

        public bool ReserveStock(int productId, int quantity)
        {
            var stock = _db.Stocks.Where(x => x.ProductId == productId && x.Quantity >= quantity).FirstOrDefault();
            if (stock != null)
            {
                stock.Quantity = stock.Quantity - quantity;
                _db.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool UpdateStock(int productId, int quantity)
        {
            var stock = _db.Stocks.Where(x => x.ProductId == productId).FirstOrDefault();
            if (stock != null)
            {
                stock.Quantity = stock.Quantity + quantity;
                _db.SaveChanges();
                return true;
            }
            else
            {
                stock = new Stock()
                {
                    ProductId = productId,
                    Quantity = quantity
                };
                _db.Stocks.Add(stock);
                _db.SaveChanges();
                return true;
            }
        }
    }
}
