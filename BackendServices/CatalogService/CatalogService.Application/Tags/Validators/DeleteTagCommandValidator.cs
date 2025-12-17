using CatalogService.Application.Tags.Commands;
using FluentValidation;

namespace CatalogService.Application.Tags.Validators
{
    public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
    {
        public DeleteTagCommandValidator()
        {
            RuleFor(x => x.TagId).GreaterThan(0);
        }
    }
}
