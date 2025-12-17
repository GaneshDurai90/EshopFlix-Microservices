using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Manufacturers.Queries
{
    public sealed class SearchManufacturersQuery : IQuery<PagedResult<ManufacturerListItemDto>>
    {
        public string? Term { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
