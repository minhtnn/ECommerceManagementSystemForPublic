using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Statistics;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class StatisticMapper : Profile
{
    public StatisticMapper()
    {
        CreateMap<DailyProductSales, GetAllProductsStaticByBrandResponse>()
            .ForMember(dest => dest.ProductId,
                opt =>
                    opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.ProductNameSnapshot,
                opt =>
                    opt.MapFrom(src => src.ProductNameSnapshot))
            .ForMember(dest => dest.ProductImagePath,
                opt =>
                    opt.MapFrom(src => src.ProductImagePath))
            .ForMember(dest => dest.SaleDate,
                opt =>
                    opt.MapFrom(src => src.SaleDate))
            .ForMember(dest => dest.TotalQuantitySold,
                opt =>
                    opt.MapFrom(src => src.TotalQuantitySold))
            .ForMember(dest => dest.TotalGiftQuantity,
                opt =>
                    opt.MapFrom(src => src.TotalGiftQuantity))
            .ForMember(dest => dest.TotalRevenueGross,
                opt =>
                    opt.MapFrom(src => src.TotalRevenueGross))
            .ForMember(dest => dest.TotalOrderCount,
                opt =>
                    opt.MapFrom(src => src.TotalOrderCount))
            .ForMember(dest => dest.ProductImageUrl,
                opt =>
                    opt.Ignore());

        CreateMap<DailyPromotionStats, GetAllPromotionRulesStaticByBrandResponse>()
            .ForMember(dest => dest.PromotionRuleId,
                opt =>
                    opt.MapFrom(src => src.PromotionRuleId))
            .ForMember(dest => dest.PromotionNameSnapshot,
                opt =>
                    opt.MapFrom(src => src.PromotionNameSnapshot))
            .ForMember(dest => dest.StatDate,
                opt =>
                    opt.MapFrom(src => src.StatDate))
            .ForMember(dest => dest.TotalDiscountIssued,
                opt =>
                    opt.MapFrom(src => src.TotalDiscountIssued))
            .ForMember(dest => dest.TotalOrdersUsed,
                opt =>
                    opt.MapFrom(src => src.TotalOrdersUsed))
            .ForMember(dest => dest.TotalRevenueWithPromo,
                opt =>
                    opt.MapFrom(src => src.TotalRevenueWithPromo));
    }
}