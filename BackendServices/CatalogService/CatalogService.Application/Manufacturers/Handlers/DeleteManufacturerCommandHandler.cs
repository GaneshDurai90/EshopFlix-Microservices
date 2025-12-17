using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Manufacturers.Handlers
{
    public sealed class DeleteManufacturerCommandHandler : ICommandHandler<DeleteManufacturerCommand, bool>
    {
        private readonly IManufacturerRepository _repository;

        public DeleteManufacturerCommandHandler(IManufacturerRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteManufacturerCommand command, CancellationToken ct)
        {
            var entity = await _repository.GetByIdAsync(command.ManufacturerId, ct);
            if (entity == null)
            {
                throw AppException.NotFound("manufacturer", $"Manufacturer {command.ManufacturerId} not found");
            }

            await _repository.DeleteAsync(entity, ct);
            return true;
        }
    }
}
