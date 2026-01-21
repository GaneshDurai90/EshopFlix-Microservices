using AutoMapper;
using CartService.Application.DTOs;
using CartService.Domain.Entities;


namespace CartService.Application.Mappers
{
    public class CartMapper : Profile
    {
        public CartMapper()
        {
            // Map CartItem first (used by Cart mapping)
            CreateMap<CartItem, CartItemDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set later from Catalog
                .ReverseMap();

            // Map Cart with explicit CartItems collection mapping
            CreateMap<Cart, CartDTO>()
                .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.CartItems))
                .ForMember(dest => dest.Total, opt => opt.Ignore())
                .ForMember(dest => dest.Tax, opt => opt.Ignore())
                .ForMember(dest => dest.GrandTotal, opt => opt.Ignore())
                .ReverseMap();
        }
    }
    
}
