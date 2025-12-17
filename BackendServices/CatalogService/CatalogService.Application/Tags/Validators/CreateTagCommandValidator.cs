using CatalogService.Application.Tags.Commands;
using FluentValidation;

namespace CatalogService.Application.Tags.Validators
{
    public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
    {
        public CreateTagCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        }
    }
}
