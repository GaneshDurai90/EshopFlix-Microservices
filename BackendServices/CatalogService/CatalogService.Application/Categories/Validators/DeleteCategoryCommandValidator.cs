using CatalogService.Application.Categories.Commands;
using FluentValidation;

namespace CatalogService.Application.Categories.Validators
{
    public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.CategoryId).GreaterThan(0);
        }
    }
}
