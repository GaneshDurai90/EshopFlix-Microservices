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
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(CatalogServiceClient catalogClient, ILogger<ProductsController> logger)
        {
            _catalogClient = catalogClient;
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

            var variants = await _catalogClient.GetProductVariantsAsync(id, ct);
            var priceHistory = await _catalogClient.GetPriceHistoryAsync(id, 8, ct);
            var reviews = await _catalogClient.GetProductReviewsAsync(id, 6, ct);
            var promotions = await _catalogClient.GetActivePromotionsAsync(3, ct);

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Variants = variants,
                PriceHistory = priceHistory,
                Reviews = reviews,
                ActivePromotions = promotions
            };

            return View(viewModel);
        }
    }
}
