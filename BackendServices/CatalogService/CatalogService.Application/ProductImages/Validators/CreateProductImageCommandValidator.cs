using CatalogService.Application.ProductImages.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductImages.Validators
{
    public sealed class CreateProductImageCommandValidator : AbstractValidator<CreateProductImageCommand>
    {
        public CreateProductImageCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.AltText).MaximumLength(500);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
