using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.ProductImages.Commands
{
    public sealed class SetPrimaryProductImageCommand : ICommand<ProductImageDto>
    {
        public int ProductId { get; init; }
        public int ProductImageId { get; init; }
    }
}
