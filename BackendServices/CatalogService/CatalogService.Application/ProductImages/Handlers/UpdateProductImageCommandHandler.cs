using System.Collections.Generic;
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
    public sealed class UpdateProductImageCommandHandler : ICommandHandler<UpdateProductImageCommand, ProductImageDto>
    {
        private readonly IProductImageRepository _imageRepository;
        private readonly IMapper _mapper;

        public UpdateProductImageCommandHandler(IProductImageRepository imageRepository, IMapper mapper)
        {
            _imageRepository = imageRepository;
            _mapper = mapper;
        }

        public async Task<ProductImageDto> Handle(UpdateProductImageCommand command, CancellationToken ct)
        {
            var image = await _imageRepository.GetByIdAsync(command.ProductImageId, ct);
            if (image == null)
            {
                throw AppException.NotFound("image", $"Image {command.ProductImageId} not found");
            }

            image.Url = command.Url;
            image.AltText = command.AltText;
            image.SortOrder = command.SortOrder;
            image.IsPrimary = command.IsPrimary;

            if (command.IsPrimary)
            {
                var images = await _imageRepository.GetByProductAsync(image.ProductId, ct);
                foreach (var existing in images)
                {
                    if (existing.ProductImageId != image.ProductImageId && existing.IsPrimary)
                    {
                        existing.IsPrimary = false;
                        await _imageRepository.UpdateAsync(existing, ct);
                    }
                }
            }

            await _imageRepository.UpdateAsync(image, ct);
            return _mapper.Map<ProductImageDto>(image);
        }
    }
}
