using CatalogService.Application.Promotions.Commands;
using FluentValidation;

namespace CatalogService.Application.Promotions.Validators
{
    public sealed class DeletePromotionCommandValidator : AbstractValidator<DeletePromotionCommand>
    {
        public DeletePromotionCommandValidator()
        {
            RuleFor(x => x.PromotionId).GreaterThan(0);
        }
    }
}
