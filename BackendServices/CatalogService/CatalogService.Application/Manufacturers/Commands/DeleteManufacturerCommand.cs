using CatalogService.Application.CQRS;

namespace CatalogService.Application.Manufacturers.Commands
{
    public sealed class DeleteManufacturerCommand : ICommand<bool>
    {
        public int ManufacturerId { get; init; }
    }
}
