using CatalogService.Application.Tags.Commands;
using FluentValidation;

namespace CatalogService.Application.Tags.Validators
{
    public sealed class AssignTagsToProductCommandValidator : AbstractValidator<AssignTagsToProductCommand>
    {
        public AssignTagsToProductCommandValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
        }
    }
}
