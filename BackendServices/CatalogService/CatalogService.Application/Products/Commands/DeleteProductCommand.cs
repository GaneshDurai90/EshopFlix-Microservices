using CatalogService.Application.CQRS;

namespace CatalogService.Application.Products.Commands
{
    public sealed class DeleteProductCommand : ICommand<bool>
    {
        public int ProductId { get; init; }
    }
}
