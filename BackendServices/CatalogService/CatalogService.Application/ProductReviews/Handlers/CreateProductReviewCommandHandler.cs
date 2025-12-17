using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.ProductReviews.Handlers
{
    public sealed class CreateProductReviewCommandHandler : ICommandHandler<CreateProductReviewCommand, ProductReviewDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public CreateProductReviewCommandHandler(
            IProductRepository productRepository,
            IProductReviewRepository reviewRepository,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _mapper = mapper;
        }

        public async Task<ProductReviewDto> Handle(CreateProductReviewCommand command, CancellationToken ct)
        {
            var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
            if (product == null)
            {
                throw AppException.NotFound("product", $"Product {command.ProductId} not found");
            }

            var review = new ProductReview
            {
                ProductId = command.ProductId,
                UserId = command.UserId,
                Rating = command.Rating,
                Title = command.Title,
                Body = command.Body,
                IsPublished = false,
                CreatedDate = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review, ct);
            return _mapper.Map<ProductReviewDto>(review);
        }
    }
}
