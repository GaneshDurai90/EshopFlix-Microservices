using System;
using System.Collections.Generic;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Promotions.Commands
{
    public sealed class CreatePromotionCommand : ICommand<PromotionDto>
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public byte DiscountType { get; init; }
        public decimal DiscountValue { get; init; }
        public bool AppliesToAllProducts { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public bool IsActive { get; init; }
        public IReadOnlyCollection<int> ProductIds { get; init; } = Array.Empty<int>();
    }
}
