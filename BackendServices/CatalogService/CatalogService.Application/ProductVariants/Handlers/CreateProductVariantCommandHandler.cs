using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.ProductVariants.Handlers
{
    public sealed class CreateProductVariantCommandHandler : ICommandHandler<CreateProductVariantCommand, ProductVariantDto>
    {
        private readonly IProductVariantRepository _variantRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public CreateProductVariantCommandHandler(
            IProductVariantRepository variantRepository,
            IProductRepository productRepository,
            IMapper mapper)
        {
            _variantRepository = variantRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<ProductVariantDto> Handle(CreateProductVariantCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            var skuExists = await _variantRepository.ExistsSkuAsync(command.Sku.Trim(), null, ct);
            if (skuExists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "SKU already exists." }
                });
            }

            var variant = new ProductVariant
            {
                ProductId = command.ProductId,
                Sku = command.Sku.Trim(),
                Barcode = command.Barcode,
                Attributes = command.Attributes,
                UnitPrice = command.UnitPrice,
                Currency = command.Currency ?? "USD",
                CompareAtPrice = command.CompareAtPrice,
                CostPrice = command.CostPrice,
                IsDefault = command.IsDefault,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            if (command.IsDefault)
            {
                var currentDefault = await _variantRepository.GetDefaultAsync(command.ProductId, ct);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    await _variantRepository.UpdateAsync(currentDefault, ct);
                }
            }

            await _variantRepository.AddAsync(variant, ct);
            return _mapper.Map<ProductVariantDto>(variant);
        }
    }
}
