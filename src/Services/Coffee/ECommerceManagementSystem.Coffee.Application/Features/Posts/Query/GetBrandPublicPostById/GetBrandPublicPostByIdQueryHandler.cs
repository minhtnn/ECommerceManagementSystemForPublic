using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Posts;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostById;

public class GetBrandPublicPostByIdQueryHandler : IRequestHandler<GetBrandPublicPostByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IMediaService _mediaService;


    public GetBrandPublicPostByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPublicPostByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cachedPosts = await _cacheService.GetDetailFromCacheAsync<GetPublicBrandPostByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public"
            );

            if (cachedPosts != null)
            {
                _logger.Debug($"Cache HIT for post:{request.Id}");
                cachedPosts.PublishedAt = cachedPosts.PublishedAt.HasValue
                    ? TimeUtil.ConvertFromUtc(cachedPosts.PublishedAt.Value, request.TimeZone)
                    : null;
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin bài đăng thành công",
                    Data = cachedPosts
                };
            }

            _logger.Debug($"Cache MISS for post:{request.Id}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var post = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .SingleOrDefaultAsync<GetPublicBrandPostByIdResponse>(
                predicate: (x =>
                    x.Id == request.Id 
                    && x.Brand.Code == request.BrandCode 
                    && x.Brand.Status == EBrandStatus.Active
                    && x.Status == EPostStatus.Published),
                include: x => x.Include(x => x.Brand)
            );
        if (post == null)
        {
            throw new BadHttpRequestException("Không tìm thấy bài đăng với ID đã cho");
        }

        if (!string.IsNullOrWhiteSpace(post.ImagePath))
        {
            try
            {
                post.ImageUrl = await _mediaService.GetImageUrlAsync(
                    post.ImagePath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to sign featured image {P}: {E}",
                    post.ImagePath, ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            try
            {
                post.Content = await _mediaService.ResolveContentImageUrlsAsync(
                    post.Content,
                    TimeSpan.FromDays(7)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to resolve inline images for post {Id}: {E}",
                    post.Id, ex.Message);
            }
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public",
                post, CacheConfig.PostsCacheTTL);
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache post {Id}: {Error}", post.Id, e.Message);
        }

        if (post != null)
        {
            post.PublishedAt = post.PublishedAt.HasValue
                ? TimeUtil.ConvertFromUtc(post.PublishedAt.Value, request.TimeZone)
                : null;
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin bài đăng thành công",
            Data = post
        };
    }
}