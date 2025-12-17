using System;

namespace CatalogService.Application.DTO
{
    public sealed class PromotionListItemDto
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
