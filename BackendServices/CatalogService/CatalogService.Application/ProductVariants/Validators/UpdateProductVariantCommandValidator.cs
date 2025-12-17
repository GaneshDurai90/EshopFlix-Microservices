using CatalogService.Application.ProductVariants.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductVariants.Validators
{
    public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
    {
        public UpdateProductVariantCommandValidator()
        {
            RuleFor(x => x.SkuId).GreaterThan(0);
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0).When(x => x.CompareAtPrice.HasValue);
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).When(x => x.CostPrice.HasValue);
        }
    }
}
