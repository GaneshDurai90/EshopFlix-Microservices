using AutoMapper;
using PaymentService.Application.DTOs;
using PaymentService.Domain.Entities;


namespace PaymentService.Application.Mappers
{
    public class PaymentMapper : Profile
    {
        public PaymentMapper()
        {
            CreateMap<PaymentConfirmDTO, PaymentDetail>();
            CreateMap<PaymentDetailDTO, PaymentDetail>();
        }
    }
}
