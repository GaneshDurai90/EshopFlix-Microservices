using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductImages.Handlers
{
    public sealed class SetPrimaryProductImageCommandHandler : ICommandHandler<SetPrimaryProductImageCommand, ProductImageDto>
    {
        private readonly IProductImageRepository _imageRepository;
        private readonly IMapper _mapper;

        public SetPrimaryProductImageCommandHandler(IProductImageRepository imageRepository, IMapper mapper)
        {
            _imageRepository = imageRepository;
            _mapper = mapper;
        }

        public async Task<ProductImageDto> Handle(SetPrimaryProductImageCommand command, CancellationToken ct)
        {
            var image = await _imageRepository.GetByIdAsync(command.ProductImageId, ct);
            if (image == null || image.ProductId != command.ProductId)
            {
                throw AppException.NotFound("image", $"Image {command.ProductImageId} not found for product {command.ProductId}");
            }

            var images = await _imageRepository.GetByProductAsync(command.ProductId, ct);
            foreach (var existing in images)
            {
                existing.IsPrimary = existing.ProductImageId == image.ProductImageId;
                await _imageRepository.UpdateAsync(existing, ct);
            }

            return _mapper.Map<ProductImageDto>(image);
        }
    }
}
