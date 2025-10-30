using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockService.Application.Services.Abstractions
{
    public interface IStockAppService
    {
        bool CheckStockAvailibility(int productId, int quantity);
        bool ReserveStock(int productId, int quantity);
        bool UpdateStock(int productId, int quantity);
    }
}
