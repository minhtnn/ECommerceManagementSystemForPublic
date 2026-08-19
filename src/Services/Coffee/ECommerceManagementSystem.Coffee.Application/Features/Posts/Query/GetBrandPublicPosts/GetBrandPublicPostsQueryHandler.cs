using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Posts;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPosts;

public class GetBrandPublicPostsQueryHandler : IRequestHandler<GetBrandPublicPostsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IMediaService _mediaService;

    public GetBrandPublicPostsQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPublicPostsQuery request, CancellationToken cancellationToken)
    {
        string? cacheKey = null;

        cacheKey = BuildCacheKey(request, request.BrandCode);
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");
                var cachedResult =
                    JsonSerializer.Deserialize<Paginate<GetPublicBrandPostsResponse>>(cachedData);
                if (cachedResult != null && cachedResult.Items.Any())
                {
                    foreach (var x in cachedResult.Items)
                    {
                        x.PublishedAt = x.PublishedAt.HasValue
                            ? TimeUtil.ConvertFromUtc(x.PublishedAt.Value, request.TimeZone)
                            : null;
                    }
                    return new ApiResponse()
                    {
                        Status = StatusCodes.Status200OK,
                        Message = "Lấy danh sách bài đăng thành công",
                        Data = cachedResult
                    };
                }
            }
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed: {Error}", e.Message);
        }

        var posts = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .GetPagingListAsync<GetPublicBrandPostsResponse>(
                predicate: x =>
                    (x.Brand.Code.Equals(request.BrandCode))
                    && (x.Brand.Status == EBrandStatus.Active)
                    && (x.Status == EPostStatus.Published),
                include: x => x.Include(x => x.Brand),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );

        foreach (var post in posts.Items)
        {
            if (post.ImagePath != null && !string.IsNullOrEmpty(post.ImagePath))
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
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        post.ImagePath,
                        ex.Message
                    );
                }
            }
        }

        // Cache ONLY first page
        if (cacheKey != null)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(posts);
                await _redisService.SetStringAsync(
                    cacheKey,
                    serializedData,
                    TimeSpan.FromMinutes(3)
                );
                _logger.Information($"Cached first page with key: {cacheKey}");
            }
            catch (RedisException redisEx)
            {
                _logger.Warning("Failed to cache posts: {Error}", redisEx.Message);
            }
        }

        if (posts != null && posts.Items.Any())
        {
            foreach (var x in posts.Items)
            {
                x.PublishedAt = x.PublishedAt.HasValue
                    ? TimeUtil.ConvertFromUtc(x.PublishedAt.Value, request.TimeZone)
                    : null;
            }
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách bài đăng thành công",
            Data = posts
        };
    }

    private string BuildCacheKey(GetBrandPublicPostsQuery request, string brandCode)
    {
        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Posts)}:{ERole.EndCustomer}:{brandCode}")}:public" +
            $":page:{request.Page}:size:{request.Size}" +
            $"sortBy:{request.SortBy}:isAsc:{request.IsAsc}";
    }
}