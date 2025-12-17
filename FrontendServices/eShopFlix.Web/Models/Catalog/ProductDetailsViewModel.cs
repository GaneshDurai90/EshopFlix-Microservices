using System.Collections.Generic;

namespace eShopFlix.Web.Models
{
    public sealed class ProductDetailsViewModel
    {
        public ProductDetailModel? Product { get; init; }
        public IReadOnlyList<ProductVariantModel> Variants { get; init; } = System.Array.Empty<ProductVariantModel>();
        public IReadOnlyList<PriceHistoryEntryModel> PriceHistory { get; init; } = System.Array.Empty<PriceHistoryEntryModel>();
        public IReadOnlyList<ProductReviewModel> Reviews { get; init; } = System.Array.Empty<ProductReviewModel>();
        public IReadOnlyList<PromotionSummaryModel> ActivePromotions { get; init; } = System.Array.Empty<PromotionSummaryModel>();
    }
}
