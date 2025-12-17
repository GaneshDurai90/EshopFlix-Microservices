using CatalogService.Application.Products.Commands;
using FluentValidation;

namespace CatalogService.Application.Products.Validators
{
    public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
    {
        public DeleteProductCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
        }
    }
}
