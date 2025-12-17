using CatalogService.Application.Categories.Commands;
using FluentValidation;

namespace CatalogService.Application.Categories.Validators
{
    public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug)
                .NotEmpty()
                .Matches("^[a-z0-9\\-]+$")
                .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
