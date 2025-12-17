using CatalogService.Application.ProductReviews.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductReviews.Validators
{
    public sealed class DeleteProductReviewCommandValidator : AbstractValidator<DeleteProductReviewCommand>
    {
        public DeleteProductReviewCommandValidator()
        {
            RuleFor(x => x.ReviewId).GreaterThan(0);
        }
    }
}
