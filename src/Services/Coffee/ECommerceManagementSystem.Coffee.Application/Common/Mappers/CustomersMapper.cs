using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.CreateCustomerAddress;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Customer;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class CustomersMapper : Profile
{
    public CustomersMapper()
    {
        #region Mapper Customers to GetCustomersResponse

        CreateMap<Customers, GetCustomersResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? ""))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                src.CustomerAccounts.FirstOrDefault().Account.Status));

        #endregion

        #region CreateCustomerAddressCommand to CustomerAddress

        CreateMap<CreateCustomerAddressCommand, CustomerAddresses>()
            .ForMember(dest => dest.Receiver,
                opt =>
                    opt.MapFrom(src => src.Receiver))
            .ForMember(dest => dest.Address,
                opt =>
                    opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.ShippingContact,
                opt =>
                    opt.MapFrom(src => src.ShippingContact))
            .ForMember(dest => dest.Latitude,
                opt =>
                    opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude,
                opt =>
                    opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.IsPrimary,
                opt =>
                    opt.MapFrom(src => src.IsPrimary));

        #endregion

        #region CustomerAddresses to GetCustomerAddressesResponse

        CreateMap<CustomerAddresses, GetCustomerAddressesResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Receiver,
                opt =>
                    opt.MapFrom(src => src.Receiver))
            .ForMember(dest => dest.Address,
                opt =>
                    opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.ShippingContact,
                opt =>
                    opt.MapFrom(src => src.ShippingContact))
            .ForMember(dest => dest.Latitude,
                opt =>
                    opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude,
                opt =>
                    opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.IsPrimary,
                opt =>
                    opt.MapFrom(src => src.IsPrimary));

        #endregion
        
        #region CustomerAddresses to GetCustomerAddressesResponse

        CreateMap<CustomerAddresses, GetCustomerAddressByIdResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Receiver,
                opt =>
                    opt.MapFrom(src => src.Receiver))
            .ForMember(dest => dest.Address,
                opt =>
                    opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.ShippingContact,
                opt =>
                    opt.MapFrom(src => src.ShippingContact))
            .ForMember(dest => dest.Latitude,
                opt =>
                    opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude,
                opt =>
                    opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.IsPrimary,
                opt =>
                    opt.MapFrom(src => src.IsPrimary))
            .ForMember(dest => dest.CreatedDate,
                opt =>
                    opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate,
                opt =>
                    opt.MapFrom(src => src.LastModifiedDate));

        #endregion
    }
}