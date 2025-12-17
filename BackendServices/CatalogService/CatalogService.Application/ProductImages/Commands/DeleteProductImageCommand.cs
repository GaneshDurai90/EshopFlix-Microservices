using CatalogService.Application.CQRS;

namespace CatalogService.Application.ProductImages.Commands
{
    public sealed class DeleteProductImageCommand : ICommand<bool>
    {
        public int ProductImageId { get; init; }
    }
}
