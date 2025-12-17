using System;
using System.Collections.Generic;

namespace CatalogService.Application.DTO
{
    public sealed class PromotionDto
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public bool AppliesToAllProducts { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<int> ProductIds { get; set; } = Array.Empty<int>();
    }
}
