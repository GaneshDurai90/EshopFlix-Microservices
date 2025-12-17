using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Products.Commands
{
    public sealed class ChangeProductStatusCommand : ICommand<ProductDetailDto>
    {
        public int ProductId { get; init; }
        public ProductStatus Status { get; init; }
    }
}
