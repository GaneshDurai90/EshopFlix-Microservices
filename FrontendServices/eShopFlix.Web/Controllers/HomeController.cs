using eShopFlix.Web.HttpClients;
using eShopFlix.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace eShopFlix.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CatalogServiceClient _catalogServiceClient;
        public HomeController(ILogger<HomeController> logger, CatalogServiceClient catalogServiceClient)
        {
            _logger = logger;
            _catalogServiceClient = catalogServiceClient;
        }

        public async Task<IActionResult> Index(string? term, int page = 1, int pageSize = 12, CancellationToken ct = default)
        {
            var summaries = await _catalogServiceClient.GetProductSummariesAsync(ct);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var filter = term.Trim().ToLowerInvariant();
                summaries = summaries
                    .Where(p => p.Name.ToLowerInvariant().Contains(filter)
                        || (!string.IsNullOrWhiteSpace(p.CategoryName) && p.CategoryName.ToLowerInvariant().Contains(filter)))
                    .ToList();
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 12 : pageSize;
            var total = summaries.Count;
            var skip = (page - 1) * pageSize;
            var paged = summaries.Skip(skip).Take(pageSize).ToList();

            var promotions = await _catalogServiceClient.GetActivePromotionsAsync(3, ct);

            var vm = new CatalogIndexViewModel
            {
                SearchTerm = term,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Promotions = promotions,
                Products = paged.Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Slug = p.Slug,
                    CategoryName = p.CategoryName,
                    ImageUrl = p.ImageUrl,
                    Price = p.DefaultPrice,
                    Currency = p.Currency,
                    Description = p.Description,
                    Status = p.Status,
                    IsSearchable = p.IsSearchable,
                    CreatedDate = p.CreatedDate
                }).ToList()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
