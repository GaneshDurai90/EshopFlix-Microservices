using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Manufacturers.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Manufacturers.Handlers
{
    public sealed class GetManufacturerByIdQueryHandler : IQueryHandler<GetManufacturerByIdQuery, ManufacturerDto?>
    {
        private readonly IManufacturerRepository _repository;
        private readonly IMapper _mapper;

        public GetManufacturerByIdQueryHandler(IManufacturerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ManufacturerDto?> Handle(GetManufacturerByIdQuery query, CancellationToken ct)
        {
            var entity = await _repository.GetByIdAsync(query.ManufacturerId, ct);
            return entity == null ? null : _mapper.Map<ManufacturerDto>(entity);
        }
    }
}
