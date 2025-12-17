using CatalogService.Application.CQRS;

namespace CatalogService.Application.Categories.Commands
{
    public sealed class DeleteCategoryCommand : ICommand<bool>
    {
        public int CategoryId { get; init; }
    }
}
