using CatalogService.Application.Categories.Commands;
using FluentValidation;

namespace CatalogService.Application.Categories.Validators
{
    public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug)
                .NotEmpty()
                .Matches("^[a-z0-9\\-]+$")
                .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
