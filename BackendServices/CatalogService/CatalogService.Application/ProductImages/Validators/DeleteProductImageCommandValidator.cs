using CatalogService.Application.ProductImages.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductImages.Validators
{
    public sealed class DeleteProductImageCommandValidator : AbstractValidator<DeleteProductImageCommand>
    {
        public DeleteProductImageCommandValidator()
        {
            RuleFor(x => x.ProductImageId).GreaterThan(0);
        }
    }
}
