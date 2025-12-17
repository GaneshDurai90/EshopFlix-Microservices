using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductReviews.Handlers
{
    public sealed class UpdateProductReviewCommandHandler : ICommandHandler<UpdateProductReviewCommand, ProductReviewDto>
    {
        private readonly IProductReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public UpdateProductReviewCommandHandler(IProductReviewRepository reviewRepository, IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _mapper = mapper;
        }

        public async Task<ProductReviewDto> Handle(UpdateProductReviewCommand command, CancellationToken ct)
        {
            var review = await _reviewRepository.GetByIdAsync(command.ReviewId, ct);
            if (review == null)
            {
                throw AppException.NotFound("review", $"Review {command.ReviewId} not found");
            }

            review.Rating = command.Rating;
            review.Title = command.Title;
            review.Body = command.Body;
            review.IsPublished = command.IsPublished;

            await _reviewRepository.UpdateAsync(review, ct);
            return _mapper.Map<ProductReviewDto>(review);
        }
    }
}
