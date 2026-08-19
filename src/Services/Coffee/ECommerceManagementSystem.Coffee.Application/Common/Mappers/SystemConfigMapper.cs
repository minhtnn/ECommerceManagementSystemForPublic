using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.SystemConfigs;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class SystemConfigMapper : Profile
{
    public SystemConfigMapper()
    {
        CreateMap<SystemConfigKeys, GetSystemConfigsResponse>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(
                    src => src.Id))
            .ForMember(dest => dest.Key,
                opt => opt.MapFrom(
                    src => src.Key))
            .ForMember(dest => dest.Title,
                opt => opt.MapFrom(
                    src => src.Title))
            .ForMember(dest => dest.DataType,
                opt => opt.MapFrom(
                    src => src.DataType))
            .ForMember(dest => dest.Description,
                opt => opt.MapFrom(
                    src => src.Description))
            .ForMember(dest => dest.IsRequired,
                opt => opt.MapFrom(
                    src => src.IsRequired))
            .ForMember(dest => dest.IsSecure,
                opt => opt.MapFrom(
                    src => src.IsSecure))
            .ForMember(dest => dest.DefaultValue,
                opt => opt.MapFrom(
                    src => src.DefaultValue))
            .ForMember(dest => dest.DisplayOrder,
                opt => opt.MapFrom(
                    src => src.DisplayOrder))
            .ForMember(dest => dest.Value,
                opt => opt.MapFrom(
                    src => src.ConfigValues.FirstOrDefault().Value ?? src.DefaultValue))
            .ForMember(dest => dest.Dependencies,
                opt => opt.MapFrom(
                    src => src.DependentDependencies));
        CreateMap<SystemConfigDependencies, SystemConfigDependencyResponse>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(
                    src => src.Id))
            .ForMember(dest => dest.TriggerKeyId,
                opt => opt.MapFrom(
                    src => src.TriggerKeyId))
            .ForMember(dest => dest.TriggerKey,
                opt => opt.MapFrom(
                    src => src.TriggerKey.Key))
            .ForMember(dest => dest.TriggerValue,
                opt => opt.MapFrom(
                    src => src.TriggerValue));
    }
}