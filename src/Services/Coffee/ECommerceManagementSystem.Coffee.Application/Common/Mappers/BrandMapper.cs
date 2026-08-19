using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.UpdateBrand;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Brands;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class BrandMapper : Profile
{
    public BrandMapper()
    {
        #region Map CreateBrandCommand to Brands

        CreateMap<CreateBrandCommand, Brands>()
            .ForMember(dest => dest.Code,
                opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address,
                opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber,
                opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Configuration,
                opt => opt.MapFrom(src => src.Configuration));

        #endregion

        #region Map Brands to GetBrandsResponse

        CreateMap<Brands, GetBrandsResponse>()
            .ForMember(dest => dest.Id, opt
                => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            // .ForMember(dest => dest.Fullname, opt =>
            //     opt.MapFrom(src => src.Fullname))
            // .ForMember(dest => dest.Slogan, opt =>
            //     opt.MapFrom(src => src.Slogan))
            .ForMember(dest => dest.Email, opt =>
                opt.MapFrom(src => src.Email))
            // .ForMember(dest => dest.Address, opt =>
            //     opt.MapFrom(src => src.Address))
            // .ForMember(dest => dest.PhoneNumber, opt =>
            //     opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.LogoPath, opt =>
                opt.MapFrom(src => src.LogoUrl))
            .ForMember(dest => dest.LogoUrl, opt =>
                opt.Ignore())
            // .ForMember(dest => dest.Configuration, opt =>
            //     opt.MapFrom(src => src.Configuration))
            ;

        #endregion

        #region Map Brands to GetBrandByIdResponse

        CreateMap<Brands, GetBrandByIdResponse>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Fullname, opt =>
                opt.MapFrom(src => src.Fullname))
            .ForMember(dest => dest.Slogan, opt =>
                opt.MapFrom(src => src.Slogan))
            .ForMember(dest => dest.Email, opt =>
                opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address, opt =>
                opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber, opt =>
                opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Configuration, opt =>
                opt.MapFrom(src => src.Configuration))
            .ForMember(dest => dest.LogoPath, opt =>
                opt.MapFrom(src => src.LogoUrl))
            .ForMember(dest => dest.LogoUrl, opt =>
                opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt =>
                opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate, opt =>
                opt.MapFrom(src => src.LastModifiedDate));

        #endregion

        #region Map Brands to GetBrandDetailsResponse

        CreateMap<Brands, GetBrandDetailsResponse>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Fullname, opt =>
                opt.MapFrom(src => src.Fullname))
            .ForMember(dest => dest.Slogan, opt =>
                opt.MapFrom(src => src.Slogan))
            .ForMember(dest => dest.Email, opt =>
                opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address, opt =>
                opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber, opt =>
                opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Configuration, opt =>
                opt.MapFrom(src => src.Configuration))
            .ForMember(dest => dest.LogoPath, opt =>
                opt.MapFrom(src => src.LogoUrl))
            .ForMember(dest => dest.LogoUrl, opt =>
                opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt =>
                opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate, opt =>
                opt.MapFrom(src => src.LastModifiedDate));

        #endregion

        #region UpdateBrandCommand to Brands

        CreateMap<UpdateBrandCommand, Brands>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Fullname,
                opt => opt.MapFrom(src => src.Fullname))
            .ForMember(dest => dest.Slogan,
                opt => opt.MapFrom(src => src.Slogan))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Address,
                opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber,
                opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.LogoUrl,
                opt => opt.Ignore())
            .ForMember(dest => dest.Configuration,
                opt => opt.MapFrom(src => src.Configuration));

        #endregion
    }
}