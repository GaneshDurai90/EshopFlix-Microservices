using CatalogService.Application.ProductReviews.Commands;
using FluentValidation;

namespace CatalogService.Application.ProductReviews.Validators
{
    public sealed class UpdateProductReviewCommandValidator : AbstractValidator<UpdateProductReviewCommand>
    {
        public UpdateProductReviewCommandValidator()
        {
            RuleFor(x => x.ReviewId).GreaterThan(0);
            RuleFor(x => x.Rating).InclusiveBetween((byte)1, (byte)5);
            RuleFor(x => x.Body).NotEmpty();
            RuleFor(x => x.Title).MaximumLength(250);
        }
    }
}
