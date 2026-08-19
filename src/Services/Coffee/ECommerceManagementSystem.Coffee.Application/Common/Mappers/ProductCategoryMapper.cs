using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class ProductCategoryMapper : Profile
{
    public ProductCategoryMapper()
    {
        #region Map CreateProductCategoryCommand to ProductCategories

        CreateMap<CreateProductCategoryCommand, ProductCategories>()
            // .ForMember(dest => dest.BrandId, opt
            //     => opt.MapFrom(src => src.BrandId))
            .ForMember(dest => dest.ParentProductCategoryId, opt
                => opt.MapFrom(src => src.ParentProductCategoryId))
            .ForMember(dest => dest.Code, opt
                => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt
                => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt
                => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.DisplayOrder, opt
                => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.ImageUrl, opt
                => opt.Ignore())
            .ForMember(dest => dest.Status, opt
                => opt.MapFrom(src => src.Status));

        #endregion
        
        #region Map ProductCategories to GetProductCategoriesResponse

        CreateMap<ProductCategories, GetProductCategoriesResponse>()
            .ForMember(dest => dest.Id, opt
                => opt.MapFrom(src => src.Id))
            // .ForMember(dest => dest.BrandId, opt
            //     => opt.MapFrom(src => src.BrandId))
            // .ForMember(dest => dest.ParentProductCategoryId, opt
            //     => opt.MapFrom(src => src.ParentProductCategoryId))
            .ForMember(dest => dest.Code, opt
                => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt
                => opt.MapFrom(src => src.Name))
            // .ForMember(dest => dest.Description, opt
            //     => opt.MapFrom(src => src.Description))
            // .ForMember(dest => dest.DisplayOrder, opt
            //     => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.Level, opt
                => opt.MapFrom(src => src.Level))
            .ForMember(dest => dest.IsLeafOnly, opt
                => opt.MapFrom(src => src.IsLeafOnly))
            // .ForMember(dest => dest.IsDeletable, opt
            //     => opt.MapFrom(src => src.IsDeletable))
            .ForMember(dest => dest.ImagePath, opt
                => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.ImageUrl, opt
                => opt.Ignore())
            .ForMember(dest => dest.Status, opt
                => opt.MapFrom(src => src.Status))
            // .ForMember(dest => dest.CreatedDate, opt
            //     => opt.MapFrom(src => src.CreatedDate))
            // .ForMember(dest => dest.LastModifiedDate, opt
            //     => opt.MapFrom(src => src.LastModifiedDate))
            ;

        #endregion
        
        #region Map ProductCategories to GetProductCategoryByIdResponse

        CreateMap<ProductCategories, GetProductCategoryByIdResponse>()
            .ForMember(dest => dest.Id, opt
                => opt.MapFrom(src => src.Id))
            // .ForMember(dest => dest.BrandId, opt
            //     => opt.MapFrom(src => src.BrandId))
            .ForMember(
                dest => dest.ParentProductCategoryName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null)
            )
            .ForMember(dest => dest.Code, opt
                => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt
                => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt
                => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.DisplayOrder, opt
                => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.Level, opt
                => opt.MapFrom(src => src.Level))
            .ForMember(dest => dest.IsLeafOnly, opt
                => opt.MapFrom(src => src.IsLeafOnly))
            .ForMember(dest => dest.IsDeletable, opt
                => opt.MapFrom(src => src.IsDeletable))
            .ForMember(dest => dest.ImagePath, opt
                => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.ImageUrl, opt
                => opt.Ignore())
            .ForMember(dest => dest.Status, opt
                => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CreatedDate, opt
                => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate, opt
                => opt.MapFrom(src => src.LastModifiedDate));

        #endregion
        
    }
}