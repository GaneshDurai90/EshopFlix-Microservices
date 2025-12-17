using CatalogService.Application.Promotions.Commands;
using FluentValidation;

namespace CatalogService.Application.Promotions.Validators
{
    public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
    {
        public CreatePromotionCommandValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.DiscountValue).GreaterThan(0);
            RuleFor(x => x.StartDate).LessThan(x => x.EndDate);
        }
    }
}
