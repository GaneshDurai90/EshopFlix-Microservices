using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Categories.Queries
{
    public sealed class GetCategoryTreeQuery : IQuery<IEnumerable<CategoryNodeDto>>
    {
        public bool IncludeInactive { get; init; }
    }
}
