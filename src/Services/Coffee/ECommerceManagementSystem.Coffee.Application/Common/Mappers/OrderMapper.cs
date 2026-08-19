using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Orders;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class OrderMapper : Profile
{
    public OrderMapper()
    {
        #region GetMyOrdersResponse
        CreateMap<Orders, GetCustomerOrdersResponse>()
            .ForMember(dest => dest.ItemCount,
                opt => opt.MapFrom(src => src.OrderDetails.Count));
        #endregion

        #region GetBrandOrdersResponse
        CreateMap<Orders, GetBrandOrdersResponse>()
            .ForMember(dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer.FullName))
            .ForMember(dest => dest.CustomerPhone,
                opt => opt.MapFrom(src => src.Customer.PhoneNumber))
            .ForMember(dest => dest.ItemCount,
                opt => opt.MapFrom(src => src.OrderDetails.Count));
        #endregion

        #region GetOrderByIdResponse
        CreateMap<Orders, GetOrderByIdResponse>()
            .ForMember(dest => dest.CustomerName,
                opt =>
                    opt.MapFrom(src => src.Customer.FullName))
            .ForMember(dest => dest.CustomerEmail,
                opt => 
                    opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerPhone,
                opt => 
                    opt.MapFrom(src => src.Customer.PhoneNumber))
            .ForMember(dest => dest.Items,
                opt => 
                    opt.MapFrom(src => src.OrderDetails))
            .ForMember(dest => dest.Payments,
                opt => 
                    opt.MapFrom(src => src.Payments))
            .ForMember(dest => dest.PaymentUrl, 
                opt => 
                    opt.MapFrom(src => src.PaymentUrl))
            .ForMember(dest => dest.QrCode, 
                opt => 
                    opt.MapFrom(src => src.QrCode));

        CreateMap<OrderDetails, OrderItemDetailResponse>();
        CreateMap<Payments, OrderPaymentResponse>();
        #endregion
    }
}