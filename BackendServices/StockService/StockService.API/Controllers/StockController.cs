using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockService.Application.Services.Abstractions;

namespace StockService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class StockController : ControllerBase
    {
        IStockAppService _stockService;
        public StockController(IStockAppService stockAppService)
        {
            _stockService = stockAppService;
        }

        [HttpGet("{productId}/{quantity}")]
        public IActionResult UpdateStock(int productId, int quantity)
        {
            var stocks = _stockService.UpdateStock(productId, quantity);
            return Ok(stocks);
        }
    }
}
