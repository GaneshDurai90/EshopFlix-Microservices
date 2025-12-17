using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductImages.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductImages.Handlers
{
    public sealed class GetProductImageByIdQueryHandler : IQueryHandler<GetProductImageByIdQuery, ProductImageDto?>
    {
        private readonly IProductImageRepository _imageRepository;
        private readonly IMapper _mapper;

        public GetProductImageByIdQueryHandler(IProductImageRepository imageRepository, IMapper mapper)
        {
            _imageRepository = imageRepository;
            _mapper = mapper;
        }

        public async Task<ProductImageDto?> Handle(GetProductImageByIdQuery query, CancellationToken ct)
        {
            var image = await _imageRepository.GetByIdAsync(query.ProductImageId, ct);
            return image == null ? null : _mapper.Map<ProductImageDto>(image);
        }
    }
}
