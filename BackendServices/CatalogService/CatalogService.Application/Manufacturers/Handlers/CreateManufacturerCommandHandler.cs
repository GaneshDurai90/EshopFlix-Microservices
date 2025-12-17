using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Manufacturers.Handlers
{
    public sealed class CreateManufacturerCommandHandler : ICommandHandler<CreateManufacturerCommand, ManufacturerDto>
    {
        private readonly IManufacturerRepository _repository;
        private readonly IMapper _mapper;

        public CreateManufacturerCommandHandler(IManufacturerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ManufacturerDto> Handle(CreateManufacturerCommand command, CancellationToken ct)
        {
            var exists = await _repository.ExistsByNameAsync(command.Name.Trim(), null, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "Manufacturer name already exists." }
                });
            }

            var entity = new Manufacturer
            {
                Name = command.Name.Trim(),
                ContactInfo = command.ContactInfo
            };

            await _repository.AddAsync(entity, ct);
            return _mapper.Map<ManufacturerDto>(entity);
        }
    }
}
