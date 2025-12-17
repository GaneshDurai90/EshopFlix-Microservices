using CatalogService.Application.ProductImages.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductImages.Validators
{
    public sealed class SetPrimaryProductImageCommandValidator : AbstractValidator<SetPrimaryProductImageCommand>
    {
        public SetPrimaryProductImageCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.ProductImageId).GreaterThan(0);
        }
    }
}
