using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;

namespace CatalogService.Application.Manufacturers.Commands
{
    public sealed class CreateManufacturerCommand : ICommand<ManufacturerDto>
    {
        public string Name { get; init; } = string.Empty;
        public string? ContactInfo { get; init; }
    }
}
