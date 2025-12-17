using CatalogService.Application.ProductImages.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductImages.Validators
{
    public sealed class UpdateProductImageCommandValidator : AbstractValidator<UpdateProductImageCommand>
    {
        public UpdateProductImageCommandValidator()
        {
            RuleFor(x => x.ProductImageId).GreaterThan(0);
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.AltText).MaximumLength(500);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
