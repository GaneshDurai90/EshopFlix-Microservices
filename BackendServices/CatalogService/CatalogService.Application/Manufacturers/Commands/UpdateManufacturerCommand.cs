using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Manufacturers.Commands
{
    public sealed class UpdateManufacturerCommand : ICommand<ManufacturerDto>
    {
        public int ManufacturerId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ContactInfo { get; init; }
    }
}
