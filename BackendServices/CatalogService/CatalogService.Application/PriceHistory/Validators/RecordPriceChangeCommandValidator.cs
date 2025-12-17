using CatalogService.Application.PriceHistory.Commands;
using FluentValidation;

namespace CatalogService.Application.PriceHistory.Validators
{
    public sealed class RecordPriceChangeCommandValidator : AbstractValidator<RecordPriceChangeCommand>
    {
        public RecordPriceChangeCommandValidator()
        {
            RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x)
                .Must(x => x.ProductId.HasValue || x.SkuId.HasValue)
                .WithMessage("Either ProductId or SkuId must be provided.");
        }
    }
}
