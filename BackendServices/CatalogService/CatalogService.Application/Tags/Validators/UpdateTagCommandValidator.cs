using CatalogService.Application.Tags.Commands;
using FluentValidation;

namespace CatalogService.Application.Tags.Validators
{
    public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
    {
        public UpdateTagCommandValidator()
        {
            RuleFor(x => x.TagId).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        }
    }
}
