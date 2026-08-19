using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdatePaymentMethod;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class PaymentMapper : Profile
{
    public PaymentMapper()
    {
        #region CreatePaymentMethod

        CreateMap<CreatePaymentMethodCommand, PaymentMethods>()
            .ForMember(x => x.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(x => x.ConfigurationSchema,
                opt =>
                    opt.MapFrom(src => src.ConfigurationSchema))
            .ForMember(x => x.Status,
                opt =>
                    opt.MapFrom(src => src.Status))
            .ForMember(x => x.ImageUrl,
                opt =>
                    opt.Ignore());

        #endregion

        #region UpdatePaymentMethod

        CreateMap<UpdatePaymentMethodCommand, PaymentMethods>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(x => x.ConfigurationSchema,
                opt =>
                    opt.MapFrom(src => src.ConfigurationSchema))
            .ForMember(x => x.Status,
                opt =>
                    opt.MapFrom(src => src.Status))
            .ForMember(x => x.ImageUrl,
                opt =>
                    opt.Ignore());

        #endregion

        #region GetPaymentMethods

        CreateMap<PaymentMethods, GetPaymentMethodsResponse>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(x => x.ImagePath,
                opt =>
                    opt.MapFrom(src => src.ImageUrl))
            .ForMember(x => x.SystemConfigurationSchema,
                opt =>
                    opt.MapFrom(src => src.ConfigurationSchema))
            .ForMember(x => x.Status,
                opt =>
                    opt.MapFrom(src => src.Status));

        #endregion

        #region GetPaymentMethodById

        CreateMap<PaymentMethods, GetPaymentMethodByIdResponse>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(x => x.ImagePath,
                opt =>
                    opt.MapFrom(src => src.ImageUrl))
            .ForMember(x => x.ConfigurationSchema,
                opt =>
                    opt.MapFrom(src => src.ConfigurationSchema))
            .ForMember(x => x.Status,
                opt =>
                    opt.MapFrom(src => src.Status));

        #endregion

        #region CreateBrandPaymentMethodCommand

        CreateMap<CreateBrandPaymentMethodCommand, BrandPaymentMethods>()
            .ForMember(dest => dest.PaymentMethodId,
                opt =>
                    opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(dest => dest.IsDefault,
                opt =>
                    opt.MapFrom(src => src.IsDefault))
            .ForMember(dest => dest.DisplayOrder,
                opt =>
                    opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.IsActive,
                opt =>
                    opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Configuration,
                opt =>
                    opt.MapFrom(src => src.Configuration));

        #endregion

        #region UpdateBrandPaymentMethod

        CreateMap<UpdateBrandPaymentMethodCommand, BrandPaymentMethods>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IsDefault,
                opt => opt.MapFrom(src => src.IsDefault))
            .ForMember(dest => dest.DisplayOrder,
                opt => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.IsActive,
                opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Configuration,
                opt => opt.MapFrom(src => src.Configuration))
            .ForMember(dest => dest.LastModifiedDate,
                opt => opt.MapFrom(src => DateTime.UtcNow));

        #endregion

        #region GetBrandPaymentMethodsResponse

        CreateMap<BrandPaymentMethods, GetBrandPaymentMethodsResponse>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.PaymentMethodId,
                opt =>
                    opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.Name))
            .ForMember(x => x.ImagePath,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.ImageUrl))
            .ForMember(x => x.IsDefault,
                opt =>
                    opt.MapFrom(src => src.IsDefault))
            .ForMember(x => x.IsActive,
                opt =>
                    opt.MapFrom(src => (src.PaymentMethods.Status == EPaymentMethodStatus.Active && src.IsActive)));

        #endregion

        #region GetBrandPaymentMethodByIdResponse

        CreateMap<BrandPaymentMethods, GetBrandPaymentMethodByIdResponse>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.PaymentMethodId,
                opt =>
                    opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.Name))
            .ForMember(x => x.ImagePath,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.ImageUrl))
            .ForMember(x => x.IsDefault,
                opt =>
                    opt.MapFrom(src => src.IsDefault))
            .ForMember(x => x.DisplayOrder,
                opt =>
                    opt.MapFrom(src => src.DisplayOrder))
            .ForMember(x => x.IsActive,
                opt =>
                    opt.MapFrom(src => (src.PaymentMethods.Status == EPaymentMethodStatus.Active && src.IsActive)))
            .ForMember(x => x.BrandConfiguration,
                opt =>
                    opt.MapFrom(src => src.Configuration))
            .ForMember(x => x.SystemConfiguration,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.ConfigurationSchema))
            .ForMember(x => x.CreatedDate,
                opt =>
                    opt.MapFrom(src => src.CreatedDate))
            .ForMember(x => x.LastModifiedDate,
                opt =>
                    opt.MapFrom(src => src.LastModifiedDate));

        #endregion

        #region GetPublicBrandPaymentMethodResponse

        CreateMap<BrandPaymentMethods, GetPublicBrandPaymentMethodResponse>()
            .ForMember(x => x.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(x => x.PaymentMethodId,
                opt =>
                    opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(x => x.BrandPaymentMethodCode,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.Code))
            .ForMember(x => x.Name,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.Name))
            .ForMember(x => x.IsDefault,
                opt =>
                    opt.MapFrom(src => src.IsDefault))
            .ForMember(x => x.ImagePath,
                opt =>
                    opt.MapFrom(src => src.PaymentMethods.ImageUrl));

        #endregion
    }
}