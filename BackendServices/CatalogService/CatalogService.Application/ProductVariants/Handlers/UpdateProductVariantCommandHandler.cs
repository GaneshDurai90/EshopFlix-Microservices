using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductVariants.Handlers
{
    public sealed class UpdateProductVariantCommandHandler : ICommandHandler<UpdateProductVariantCommand, ProductVariantDto>
    {
        private readonly IProductVariantRepository _variantRepository;
        private readonly IMapper _mapper;

        public UpdateProductVariantCommandHandler(IProductVariantRepository variantRepository, IMapper mapper)
        {
            _variantRepository = variantRepository;
            _mapper = mapper;
        }

        public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand command, CancellationToken ct)
        {
            var variant = await _variantRepository.GetByIdAsync(command.SkuId, ct);
            if (variant == null)
            {
                throw AppException.NotFound("variant", $"Variant {command.SkuId} not found");
            }

            var exists = await _variantRepository.ExistsSkuAsync(command.Sku.Trim(), command.SkuId, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "SKU already exists." }
                });
            }

            variant.Sku = command.Sku.Trim();
            variant.Barcode = command.Barcode;
            variant.Attributes = command.Attributes;
            variant.UnitPrice = command.UnitPrice;
            variant.Currency = command.Currency ?? variant.Currency;
            variant.CompareAtPrice = command.CompareAtPrice;
            variant.CostPrice = command.CostPrice;
            variant.IsDefault = command.IsDefault;
            variant.LastModifiedDate = System.DateTime.UtcNow;

            if (command.IsDefault)
            {
                var currentDefault = await _variantRepository.GetDefaultAsync(variant.ProductId, ct);
                if (currentDefault != null && currentDefault.SkuId != variant.SkuId)
                {
                    currentDefault.IsDefault = false;
                    await _variantRepository.UpdateAsync(currentDefault, ct);
                }
            }

            await _variantRepository.UpdateAsync(variant, ct);
            return _mapper.Map<ProductVariantDto>(variant);
        }
    }
}
