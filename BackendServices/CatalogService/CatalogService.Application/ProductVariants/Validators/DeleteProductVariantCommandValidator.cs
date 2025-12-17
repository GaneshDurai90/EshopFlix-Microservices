using CatalogService.Application.ProductVariants.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductVariants.Validators
{
    public sealed class DeleteProductVariantCommandValidator : AbstractValidator<DeleteProductVariantCommand>
    {
        public DeleteProductVariantCommandValidator()
        {
            RuleFor(x => x.SkuId).GreaterThan(0);
        }
    }
}
