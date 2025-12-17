using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductVariants.Handlers
{
    public sealed class DeleteProductVariantCommandHandler : ICommandHandler<DeleteProductVariantCommand, bool>
    {
        private readonly IProductVariantRepository _variantRepository;

        public DeleteProductVariantCommandHandler(IProductVariantRepository variantRepository)
        {
            _variantRepository = variantRepository;
        }

        public async Task<bool> Handle(DeleteProductVariantCommand command, CancellationToken ct)
        {
            var variant = await _variantRepository.GetByIdAsync(command.SkuId, ct);
            if (variant == null)
            {
                throw AppException.NotFound("variant", $"Variant {command.SkuId} not found");
            }

            await _variantRepository.DeleteAsync(variant, ct);
            return true;
        }
    }
}
