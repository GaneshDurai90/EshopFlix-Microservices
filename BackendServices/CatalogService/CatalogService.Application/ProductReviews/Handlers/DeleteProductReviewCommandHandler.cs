using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductReviews.Handlers
{
    public sealed class DeleteProductReviewCommandHandler : ICommandHandler<DeleteProductReviewCommand, bool>
    {
        private readonly IProductReviewRepository _reviewRepository;

        public DeleteProductReviewCommandHandler(IProductReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task<bool> Handle(DeleteProductReviewCommand command, CancellationToken ct)
        {
            var review = await _reviewRepository.GetByIdAsync(command.ReviewId, ct);
            if (review == null)
            {
                throw AppException.NotFound("review", $"Review {command.ReviewId} not found");
            }

            await _reviewRepository.DeleteAsync(review, ct);
            return true;
        }
    }
}
