using AutoMapper;
using CatalogService.Application.DTO;
using CatalogService.Domain.Entities;
using PriceHistoryEntity = CatalogService.Domain.Entities.PriceHistory;

namespace CatalogService.Application.Mappers
{
    public class ProductMapper : Profile
    {
        public ProductMapper()
        {
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ShortDescription))
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src => src.PrimaryImageUrl));

            CreateMap<Product, ProductDetailDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.PrimaryImageUrl, opt => opt.MapFrom(src => src.PrimaryImageUrl));

            CreateMap<Product, ProductListItemDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

            CreateMap<Category, CategoryDto>();
            CreateMap<Category, CategoryNodeDto>()
                .ForMember(dest => dest.Children, opt => opt.Ignore());

            CreateMap<Manufacturer, ManufacturerDto>();
            CreateMap<Manufacturer, ManufacturerListItemDto>();

            CreateMap<PriceHistoryEntity, PriceHistoryEntryDto>();

            CreateMap<ProductVariant, ProductVariantDto>();
            CreateMap<ProductVariant, ProductVariantListItemDto>();

            CreateMap<ProductImage, ProductImageDto>();

            CreateMap<ProductReview, ProductReviewDto>();
            CreateMap<ProductReview, ProductReviewListItemDto>();

            CreateMap<Promotion, PromotionDto>()
                .ForMember(dest => dest.ProductIds, opt => opt.Ignore());
            CreateMap<Promotion, PromotionListItemDto>();

            CreateMap<Tag, TagDto>();
            CreateMap<Tag, TagListItemDto>()
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0));

            CreateMap<ProductRelationship, ProductRelationshipDto>();
        }
    }
}
