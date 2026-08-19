using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Command.CreateProduct;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Products;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class ProductMapper : Profile
{
    public ProductMapper()
    {
        #region Map CreateProductCommand to Products

        CreateMap<CreateProductCommand, Products>()
            .ForMember(dest => dest.ProductCategoryId, opt =>
                opt.MapFrom(src => src.ProductCategoryId))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.FullName, opt =>
                opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Description, opt =>
                opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt =>
                opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.DisplayOrder, opt =>
                opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.ProductSellType, opt =>
                opt.MapFrom(src => src.ProductSellType))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StockQuantity, opt =>
                opt.MapFrom(src => src.StockQuantity));

        #endregion

        #region Map Products to GetProductsResponse

        CreateMap<Products, GetProductsResponse>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductCategoryId, opt =>
                opt.MapFrom(src => src.ProductCategoryId))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.FullName, opt =>
                opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Description, opt =>
                opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt =>
                opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.ProductSellType, opt =>
                opt.MapFrom(src => src.ProductSellType))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StockQuantity, opt =>
                opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.MainImagePath, opt =>
                opt.MapFrom(src => 
                    src.ProductImages.FirstOrDefault(pi => pi.IsMainImage).ImageUrl))
            .ForMember(dest => dest.MainImageAltText, opt =>
                opt.MapFrom(src => 
                    src.ProductImages.FirstOrDefault(pi => pi.IsMainImage).AltText))
            .ForMember(dest => dest.MainImageUrl, opt =>
                opt.Ignore());

        #endregion

        #region Map Products to GetProductByIdResponse

        CreateMap<Products, GetProductByIdResponse>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductCategoryName, opt =>
                opt.MapFrom(src => src.ProductCategory != null ? src.ProductCategory.Name : null))
            .ForMember(dest => dest.Code, opt =>
                opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.FullName, opt =>
                opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Description, opt =>
                opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt =>
                opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.ProductSellType, opt =>
                opt.MapFrom(src => src.ProductSellType))
            .ForMember(dest => dest.Status, opt =>
                opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.DisplayOrder, opt =>
                opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.StockQuantity, opt =>
                opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.CreatedDate, opt =>
                opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate, opt =>
                opt.MapFrom(src => src.LastModifiedDate))
            .ForMember(dest => dest.GetProductImagesResponse, opt =>
                opt.MapFrom(src => src.ProductImages))
            .ForMember(dest => dest.GetProductSideAttributesResponse, opt =>
                opt.MapFrom(src => src.ProductSideAttributes));

        CreateMap<ProductImages, GetProductByIdImagesResponse>()
            .ForMember(dest => dest.IsMainImage, opt =>
                opt.MapFrom(src => src.IsMainImage))
            .ForMember(dest => dest.ImagePath, opt =>
                opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.ImageUrl, opt =>
                opt.Ignore())
            .ForMember(dest => dest.AltText, opt =>
                opt.MapFrom(src => src.AltText))
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            ;
        CreateMap<ProductSideAttributes, GetProductByIdSideAttributesResponse>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Key, opt =>
                opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.Value, opt =>
                opt.MapFrom(src => src.Value));

        #endregion
    }
}