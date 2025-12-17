using CatalogService.Application.ProductVariants.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductVariants.Validators
{
    public sealed class SetDefaultProductVariantCommandValidator : AbstractValidator<SetDefaultProductVariantCommand>
    {
        public SetDefaultProductVariantCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.SkuId).GreaterThan(0);
        }
    }
}
