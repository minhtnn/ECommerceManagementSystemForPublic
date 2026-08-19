using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;
using ECommerceManagementSystem.Coffee.Domain.Entities;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class AccountMapper : Profile
{
    public AccountMapper()
    {
        CreateMap<CreateAccountCommand, Accounts>()
            // .ForMember(dest => dest.Role,
            //     opt => opt.MapFrom(src => src.Role))
            // .ForMember(dest => dest.PhoneNumber,
            //     opt => opt.MapFrom(src => src.PhoneNumber))
            // .ForMember(dest => dest.Email,
            //     opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Username,
                opt => opt.MapFrom(src => src.Username));
    }
}