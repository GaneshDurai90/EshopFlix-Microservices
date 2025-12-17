using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.ProductImages.Handlers
{
    public sealed class CreateProductImageCommandHandler : ICommandHandler<CreateProductImageCommand, ProductImageDto>
    {
        private readonly IProductImageRepository _imageRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductVariantRepository _variantRepository;
        private readonly IMapper _mapper;

        public CreateProductImageCommandHandler(
            IProductImageRepository imageRepository,
            IProductRepository productRepository,
            IProductVariantRepository variantRepository,
            IMapper mapper)
        {
            _imageRepository = imageRepository;
            _productRepository = productRepository;
            _variantRepository = variantRepository;
            _mapper = mapper;
        }

        public async Task<ProductImageDto> Handle(CreateProductImageCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            if (command.SkuId.HasValue)
            {
                var variant = await _variantRepository.GetByIdAsync(command.SkuId.Value, ct);
                if (variant == null || variant.ProductId != command.ProductId)
                {
                    throw AppException.Validation(new Dictionary<string, string[]>
                    {
                        ["skuId"] = new[] { "Variant not associated with product." }
                    });
                }
            }

            var image = new ProductImage
            {
                ProductId = command.ProductId,
                SkuId = command.SkuId,
                Url = command.Url,
                AltText = command.AltText,
                SortOrder = command.SortOrder,
                IsPrimary = command.IsPrimary,
                CreatedDate = DateTime.UtcNow
            };

            if (command.IsPrimary)
            {
                var images = await _imageRepository.GetByProductAsync(command.ProductId, ct);
                foreach (var existing in images)
                {
                    if (existing.IsPrimary)
                    {
                        existing.IsPrimary = false;
                        await _imageRepository.UpdateAsync(existing, ct);
                    }
                }
            }

            await _imageRepository.AddAsync(image, ct);
            return _mapper.Map<ProductImageDto>(image);
        }
    }
}
