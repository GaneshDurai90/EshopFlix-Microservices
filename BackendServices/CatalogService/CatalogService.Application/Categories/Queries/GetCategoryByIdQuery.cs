using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Categories.Queries
{
    public sealed class GetCategoryByIdQuery : IQuery<CategoryDto?>
    {
        public int CategoryId { get; init; }
    }
}
