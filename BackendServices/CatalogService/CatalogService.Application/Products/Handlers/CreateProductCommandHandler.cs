using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.IntegrationEvents;
using CatalogService.Application.Messaging;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Products.Handlers
{
    public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IIntegrationEventPublisher _eventPublisher;

        public CreateProductCommandHandler(
            IProductRepository productRepository,
            IMapper mapper,
            IIntegrationEventPublisher eventPublisher)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
        }

        public async Task<ProductDetailDto> Handle(CreateProductCommand command, CancellationToken ct)
        {
            var normalizedSlug = NormalizeSlug(command.Slug);
            var slugExists = await _productRepository.ExistsWithSlugAsync(normalizedSlug, null, ct);
            if (slugExists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Slug is already in use." }
                });
            }

            var status = Enum.IsDefined(typeof(ProductStatus), (int)command.Status)
                ? (ProductStatus)command.Status
                : ProductStatus.Draft;

            var product = new Product
            {
                Name = command.Name.Trim(),
                Slug = normalizedSlug,
                ShortDescription = command.ShortDescription,
                LongDescription = command.LongDescription,
                BrandId = command.BrandId,
                ManufacturerId = command.ManufacturerId,
                CategoryId = command.CategoryId,
                IsSearchable = command.IsSearchable,
                Weight = command.Weight,
                Dimensions = command.Dimensions,
                PrimaryImageUrl = command.PrimaryImageUrl,
                SeoTitle = command.SeoTitle,
                SeoDescription = command.SeoDescription,
                SeoKeywords = command.SeoKeywords,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
            product.SetStatus(status);

            await _productRepository.AddAsync(product, ct);

            var dto = _mapper.Map<ProductDetailDto>(product);
            await _eventPublisher.EnqueueAsync(
                nameof(ProductCreatedIntegrationEvent),
                new ProductCreatedIntegrationEvent(product.ProductId, product.Name, product.Slug, product.CategoryId),
                ct);

            return dto;
        }

        private static string NormalizeSlug(string slug)
            => slug?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
