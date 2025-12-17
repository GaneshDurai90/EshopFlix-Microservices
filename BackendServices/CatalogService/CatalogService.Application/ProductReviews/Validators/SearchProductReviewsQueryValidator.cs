using CatalogService.Application.ProductReviews.Queries;
using FluentValidation;

namespace CatalogService.Application.ProductReviews.Validators
{
    public sealed class SearchProductReviewsQueryValidator : AbstractValidator<SearchProductReviewsQuery>
    {
        public SearchProductReviewsQueryValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(200);
        }
    }
}
