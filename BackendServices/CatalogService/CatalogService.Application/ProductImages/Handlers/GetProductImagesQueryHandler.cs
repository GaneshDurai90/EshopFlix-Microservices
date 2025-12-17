using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductImages.Queries;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductImages.Handlers
{
    public sealed class GetProductImagesQueryHandler : IQueryHandler<GetProductImagesQuery, IReadOnlyList<ProductImageDto>>
    {
        private readonly IProductImageRepository _imageRepository;
        private readonly IMapper _mapper;

        public GetProductImagesQueryHandler(IProductImageRepository imageRepository, IMapper mapper)
        {
            _imageRepository = imageRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProductImageDto>> Handle(GetProductImagesQuery query, CancellationToken ct)
        {
            var images = await _imageRepository.GetByProductAsync(query.ProductId, ct);
            return _mapper.Map<IReadOnlyList<ProductImageDto>>(images);
        }
    }
}
