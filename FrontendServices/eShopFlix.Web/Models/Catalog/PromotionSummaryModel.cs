using System;

namespace eShopFlix.Web.Models
{
    public sealed class PromotionSummaryModel
    {
        public int PromotionId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string? Description { get; init; }
        public decimal? DiscountValue { get; init; }
        public byte? DiscountType { get; init; }
    }
}
