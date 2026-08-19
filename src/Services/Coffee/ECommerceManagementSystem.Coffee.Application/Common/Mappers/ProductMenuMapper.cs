using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Menus;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class ProductMenuMapper : Profile
{
    public ProductMenuMapper()
    {
        #region Map ProductCategories to GetMenuProductCategoryResponse

        CreateMap<ProductCategories, GetPublicMenuProductCategoryResponse>()
            .ForMember(dest => dest.Id, 
                opt => opt
                    .MapFrom(src => src.Id))
            .ForMember(dest => dest.ParentProductCategoryId, 
                opt => opt
                    .MapFrom(src => src.ParentProductCategoryId))
            .ForMember(dest => dest.ImagePath, 
                opt => opt
                    .MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.ImageUrl, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Name, 
                opt => opt
                    .MapFrom(src => src.Name))
            .ForMember(dest => dest.IsSelected, 
                opt => opt.Ignore())
            .ForMember(dest => dest.DisplayOrder, 
                opt => opt
                    .MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.ProductCount, 
                opt => opt.Ignore())
            .ForMember(dest => dest.TotalProductCount, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Children, 
                opt => opt.Ignore());

        #endregion

        #region Map Products to GetMenuProductResponse

        CreateMap<Products, GetPublicMenuProductResponse>()
            .ForMember(dest => dest.Id, 
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, 
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.FullName, 
                opt =>
                    opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Description, 
                opt =>
                    opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, 
                opt =>
                    opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.StockQuantity, 
                opt =>
                    opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.ProductCategoryId, 
                opt =>
                    opt.MapFrom(src => src.ProductCategoryId))
            // .ForMember(dest => dest.Status, 
            //     opt =>
            //         opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Images, 
                opt =>
                    opt.MapFrom(src => src.ProductImages))
            ;

        #endregion

        #region Map ProductImages to GetMenuProductImageResponse

        CreateMap<ProductImages, GetPublicMenuProductImageResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IsMainImage,
                opt =>
                    opt.MapFrom(src => src.IsMainImage))
            .ForMember(dest => dest.AltText,
                opt =>
                    opt.MapFrom(src => src.AltText))
            .ForMember(dest => dest.Path,
                opt =>
                    opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.Url,
                opt =>
                    opt.Ignore());

        #endregion
    }
}