using System.Threading;
using System.Threading.Tasks;
using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace eShopFlix.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CatalogServiceClient _catalogClient;
        private readonly StockServiceClient _stockClient;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            CatalogServiceClient catalogClient, 
            StockServiceClient stockClient,
            ILogger<ProductsController> logger)
        {
            _catalogClient = catalogClient;
            _stockClient = stockClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var product = await _catalogClient.GetProductDetailAsync(id, ct);
            if (product is null)
            {
                _logger.LogWarning("Product {ProductId} not found in CatalogService", id);
                return NotFound();
            }

            // Fetch data in parallel for performance
            var variantsTask = _catalogClient.GetProductVariantsAsync(id, ct);
            var priceHistoryTask = _catalogClient.GetPriceHistoryAsync(id, 8, ct);
            var reviewsTask = _catalogClient.GetProductReviewsAsync(id, 6, ct);
            var promotionsTask = _catalogClient.GetActivePromotionsAsync(3, ct);
            var stockTask = _stockClient.GetAvailabilityAsync(id, null, ct);

            await Task.WhenAll(variantsTask, priceHistoryTask, reviewsTask, promotionsTask, stockTask);

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Variants = await variantsTask,
                PriceHistory = await priceHistoryTask,
                Reviews = await reviewsTask,
                ActivePromotions = await promotionsTask,
                StockAvailability = await stockTask
            };

            return View(viewModel);
        }

        /// <summary>
        /// AJAX endpoint to check stock availability for a specific quantity.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckStock(int productId, int quantity, int? variationId, CancellationToken ct)
        {
            var availability = await _stockClient.CheckAvailabilityAsync(productId, quantity, variationId, ct);
            return Json(availability);
        }
    }
}
