using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.Mappers;
using CatalogService.Application.Messaging;
using CatalogService.Application.Products.Handlers;
using CatalogService.Application.Repositories;
using CatalogService.Application.Services.Abstractions;
using CatalogService.Application.Services.Implementation;
using CatalogService.Application.PriceHistory.Handlers;
using CatalogService.Infrastructure.Messaging;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Persistence.Repositories;
using CatalogService.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace CatalogService.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // DbContext
            

            services.AddDbContext<CatalogServiceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DbConnection")));

            services.AddScoped<IDispatcher, Dispatcher>();

            var applicationAssembly = typeof(CreateProductCommandHandler).Assembly;

            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly)
                .AddClasses(classes => classes.AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
            services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
            services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IProductRelationshipRepository, ProductRelationshipRepository>();

            services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
            services.AddScoped<IIdempotentAppRequest, IdempotentRequestStore>();

            services.AddScoped<IProductAppService, ProductAppService>();
            services.AddScoped<ICategoryAppService, CategoryAppService>();
            services.AddScoped<IManufacturerAppService, ManufacturerAppService>();
            services.AddScoped<IPriceHistoryAppService, PriceHistoryAppService>();
            services.AddScoped<IProductVariantAppService, ProductVariantAppService>();
            services.AddScoped<IProductImageAppService, ProductImageAppService>();
            services.AddScoped<IProductReviewAppService, ProductReviewAppService>();
            services.AddScoped<IPromotionAppService, PromotionAppService>();
            services.AddScoped<ITagAppService, TagAppService>();
            services.AddScoped<IProductRelationshipAppService, ProductRelationshipAppService>();
            services.AddScoped<IIdempotencyAppService, IdempotencyAppService>();

            services.AddAutoMapper(cfg => cfg.AddProfile<ProductMapper>(), applicationAssembly);
        }
    }
}
