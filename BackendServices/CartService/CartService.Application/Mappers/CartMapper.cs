using AutoMapper;
using CartService.Application.DTOs;
using CartService.Domain.Entities;


namespace CartService.Application.Mappers
{
    public class CartMapper : Profile
    {
        public CartMapper()
        {
            CreateMap<Cart,CartDTO>().ReverseMap();
            CreateMap<CartItem, CartItemDTO>().ReverseMap();
           // CreateMap<CartSnapshotDto, CartSnapshotDTO>();

        }
    }
    
}
