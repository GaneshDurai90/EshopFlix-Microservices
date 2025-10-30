using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;


namespace StockService.Application.Services.Implementations
{
    public class StockAppService : IStockAppService
    {
        IStockRepository _stockRepository;
        public StockAppService(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }
        public bool CheckStockAvailibility(int productId, int quantity)
        {
            return _stockRepository.CheckStockAvailibility(productId, quantity);
        }

        public bool ReserveStock(int productId, int quantity)
        {
            return _stockRepository.ReserveStock(productId, quantity);
        }

        public bool UpdateStock(int productId, int quantity)
        {
            return _stockRepository.UpdateStock(productId, quantity);
        }
    }
}
