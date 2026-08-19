using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.UpdateBrandPost;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Posts;

namespace ECommerceManagementSystem.Coffee.Application.Common.Mappers;

public class PostMapper : Profile
{
    public PostMapper()
    {
        CreateMap<CreateBrandPostCommand, Posts>()
            .ForMember(dest => dest.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Slug,
                opt =>
                    opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Content,
                opt =>
                    opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Excerpt,
                opt =>
                    opt.MapFrom(src => src.Excerpt))
            ;

        CreateMap<UpdateBrandPostCommand, Posts>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Slug,
                opt =>
                    opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Content,
                opt =>
                    opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Excerpt,
                opt =>
                    opt.MapFrom(src => src.Excerpt))
            .ForMember(dest => dest.Status,
                opt =>
                    opt.MapFrom(src => src.Status))
            ;

        CreateMap<Posts, GetBrandPostByIdResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Author,
                opt =>
                    opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Slug,
                opt =>
                    opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Content,
                opt =>
                    opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Excerpt,
                opt =>
                    opt.MapFrom(src => src.Excerpt))
            .ForMember(dest => dest.Status,
                opt =>
                    opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.ImagePath,
                opt =>
                    opt.MapFrom(src => src.FeaturedImage))
            .ForMember(dest => dest.ImageUrl,
                opt =>
                    opt.Ignore())
            .ForMember(dest => dest.PublishedAt,
                opt =>
                    opt.MapFrom(src => src.PublishedAt))
            .ForMember(dest => dest.CreatedDate,
                opt =>
                    opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastModifiedDate,
                opt =>
                    opt.MapFrom(src => src.LastModifiedDate))
            ;
        CreateMap<Posts, GetPublicBrandPostByIdResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Author,
                opt =>
                    opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Slug,
                opt =>
                    opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Content,
                opt =>
                    opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Excerpt,
                opt =>
                    opt.MapFrom(src => src.Excerpt))
            .ForMember(dest => dest.ImagePath,
                opt =>
                    opt.MapFrom(src => src.FeaturedImage))
            .ForMember(dest => dest.ImageUrl,
                opt =>
                    opt.Ignore())
            .ForMember(dest => dest.PublishedAt,
                opt =>
                    opt.MapFrom(src => src.PublishedAt));

        CreateMap<Posts, GetBrandPostsResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code,
                opt =>
                    opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.ImagePath,
                opt =>
                    opt.MapFrom(src => src.FeaturedImage))
            .ForMember(dest => dest.ImageUrl,
                opt =>
                    opt.Ignore())
            .ForMember(dest => dest.Status,
                opt =>
                    opt.MapFrom(src => src.Status));
        CreateMap<Posts, GetPublicBrandPostsResponse>()
            .ForMember(dest => dest.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            // .ForMember(dest => dest.Code,
            //     opt =>
            //         opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Title,
                opt =>
                    opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Author,
                opt =>
                    opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Excerpt,
                opt =>
                    opt.MapFrom(src => src.Excerpt))
            .ForMember(dest => dest.PublishedAt,
                opt =>
                    opt.MapFrom(src => src.PublishedAt))
            .ForMember(dest => dest.ImagePath,
                opt =>
                    opt.MapFrom(src => src.FeaturedImage))
            .ForMember(dest => dest.ImageUrl,
                opt =>
                    opt.Ignore());
    }
}