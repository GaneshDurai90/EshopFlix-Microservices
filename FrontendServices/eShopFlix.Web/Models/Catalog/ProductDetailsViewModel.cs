using System.Collections.Generic;
using eShopFlix.Web.Models.Stock;

namespace eShopFlix.Web.Models
{
    public sealed class ProductDetailsViewModel
    {
        public ProductDetailModel? Product { get; init; }
        public IReadOnlyList<ProductVariantModel> Variants { get; init; } = System.Array.Empty<ProductVariantModel>();
        public IReadOnlyList<PriceHistoryEntryModel> PriceHistory { get; init; } = System.Array.Empty<PriceHistoryEntryModel>();
        public IReadOnlyList<ProductReviewModel> Reviews { get; init; } = System.Array.Empty<ProductReviewModel>();
        public IReadOnlyList<PromotionSummaryModel> ActivePromotions { get; init; } = System.Array.Empty<PromotionSummaryModel>();
        
        /// <summary>
        /// Stock availability information from StockService.
        /// </summary>
        public StockAvailabilityModel? StockAvailability { get; init; }
        
        /// <summary>
        /// Whether the product can be added to cart (has stock available).
        /// </summary>
        public bool CanAddToCart => StockAvailability?.IsInStock ?? true; // Default to true if stock info unavailable
    }
}
