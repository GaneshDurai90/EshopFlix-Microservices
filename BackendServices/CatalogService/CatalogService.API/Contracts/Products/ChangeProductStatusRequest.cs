using CatalogService.Domain.Enums;

namespace CatalogService.API.Contracts.Products
{
    public sealed class ChangeProductStatusRequest
    {
        public ProductStatus Status { get; set; }
    }
}
