using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockService.Application.Repositories
{
    public interface IStockRepository
    {
        bool CheckStockAvailibility(int productId, int quantity);
        bool ReserveStock(int productId, int quantity);
        bool UpdateStock(int productId, int quantity);
    }
}
