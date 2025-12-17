using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Manufacturers.Handlers
{
    public sealed class UpdateManufacturerCommandHandler : ICommandHandler<UpdateManufacturerCommand, ManufacturerDto>
    {
        private readonly IManufacturerRepository _repository;
        private readonly IMapper _mapper;

        public UpdateManufacturerCommandHandler(IManufacturerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ManufacturerDto> Handle(UpdateManufacturerCommand command, CancellationToken ct)
        {
            var entity = await _repository.GetByIdAsync(command.ManufacturerId, ct);
            if (entity == null)
            {
                throw AppException.NotFound("manufacturer", $"Manufacturer {command.ManufacturerId} not found");
            }

            var exists = await _repository.ExistsByNameAsync(command.Name.Trim(), command.ManufacturerId, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "Manufacturer name already exists." }
                });
            }

            entity.Name = command.Name.Trim();
            entity.ContactInfo = command.ContactInfo;

            await _repository.UpdateAsync(entity, ct);
            return _mapper.Map<ManufacturerDto>(entity);
        }
    }
}
