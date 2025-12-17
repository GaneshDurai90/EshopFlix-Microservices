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
    public sealed class SetDefaultProductVariantCommandHandler : ICommandHandler<SetDefaultProductVariantCommand, ProductVariantDto>
    {
        private readonly IProductVariantRepository _variantRepository;
        private readonly IMapper _mapper;

        public SetDefaultProductVariantCommandHandler(IProductVariantRepository variantRepository, IMapper mapper)
        {
            _variantRepository = variantRepository;
            _mapper = mapper;
        }

        public async Task<ProductVariantDto> Handle(SetDefaultProductVariantCommand command, CancellationToken ct)
        {
            var variant = await _variantRepository.GetByIdAsync(command.SkuId, ct);
            if (variant == null || variant.ProductId != command.ProductId)
            {
                throw AppException.NotFound("variant", $"Variant {command.SkuId} not found for product {command.ProductId}");
            }

            var currentDefault = await _variantRepository.GetDefaultAsync(command.ProductId, ct);
            if (currentDefault != null && currentDefault.SkuId != variant.SkuId)
            {
                currentDefault.IsDefault = false;
                await _variantRepository.UpdateAsync(currentDefault, ct);
            }

            variant.IsDefault = true;
            await _variantRepository.UpdateAsync(variant, ct);

            return _mapper.Map<ProductVariantDto>(variant);
        }
    }
}
