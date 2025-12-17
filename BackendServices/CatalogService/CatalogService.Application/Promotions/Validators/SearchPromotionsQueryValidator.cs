using CatalogService.Application.Promotions.Queries;
using FluentValidation;

namespace CatalogService.Application.Promotions.Validators
{
    public sealed class SearchPromotionsQueryValidator : AbstractValidator<SearchPromotionsQuery>
    {
        public SearchPromotionsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(200);
        }
    }
}
