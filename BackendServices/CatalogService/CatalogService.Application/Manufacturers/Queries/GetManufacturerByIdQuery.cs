using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Manufacturers.Queries
{
    public sealed class GetManufacturerByIdQuery : IQuery<ManufacturerDto?>
    {
        public int ManufacturerId { get; init; }
    }
}
