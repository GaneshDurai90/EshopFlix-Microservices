using CatalogService.Application.PriceHistory.Queries;
using FluentValidation;

namespace CatalogService.Application.PriceHistory.Validators
{
    public sealed class GetPriceHistoryQueryValidator : AbstractValidator<GetPriceHistoryQuery>
    {
        public GetPriceHistoryQueryValidator()
        {
            RuleFor(x => x)
                .Must(x => x.ProductId.HasValue || x.SkuId.HasValue)
                .WithMessage("Either ProductId or SkuId must be provided.");

            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(200);
        }
    }
}
