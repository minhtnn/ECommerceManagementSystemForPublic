using System.Text.Json;
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
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPosts;

public class GetBrandPostsQueryHandler : IRequestHandler<GetBrandPostsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetBrandPostsQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IRedisService redisService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPostsQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        #region Convert timezone → UTC

        if (request.FromDate.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.TimeZone))
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Vui lòng cung cấp TimeZone khi truyền StartDate/EndDate"
                };
            }
            try
            {
                request.FromDate = TimeUtil.ConvertToUtc(request.FromDate, request.TimeZone);
            }
            catch (ArgumentException ex)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = ex.Message // "Timezone '...' không hợp lệ"
                };
            }
        }
        
        if (request.ToDate.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.TimeZone))
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Vui lòng cung cấp TimeZone khi truyền StartDate/EndDate"
                };
            }
            try
            {
                request.ToDate = TimeUtil.ConvertToUtc(request.ToDate, request.TimeZone);
            }
            catch (ArgumentException ex)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = ex.Message // "Timezone '...' không hợp lệ"
                };
            }
        }

        #endregion

        var cacheKey = BuildCacheKey(request, role, brandId.ToString());

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetBrandPostsResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách bài đăng thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var posts = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .GetPagingListAsync<GetBrandPostsResponse>(
                predicate: x => ((string.IsNullOrEmpty(request.Code) || x.Code.Contains(request.Code))
                                 && (request.Status == null || x.Status == request.Status)
                                 && (request.FromDate == null || x.CreatedDate.Date >= request.FromDate.Value.Date)
                                 && (request.ToDate == null || x.CreatedDate.Date <= request.ToDate.Value.Date)
                                 && (x.BrandId == brandId)),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );

        #region Assign image

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

        #endregion

        try
        {
            var serializedData = JsonSerializer.Serialize(posts);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PostsCacheTTL
            );
            _logger.Information(
                $"Cached  posts list with key: {cacheKey}, TTL: {CacheConfig.PostsCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache  posts list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách bài đăng thành công",
            Data = posts
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: s:list:{name}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetBrandPostsQuery request, ERole role, string brandId)
    {
        var code = string.IsNullOrEmpty(request.Code) ? "all" : request.Code;
        var status = (request.Status == null) ? "all" : request.Status.ToString();
        var sortBy = request.SortBy ?? "CreatedDate";
        var fromDate = request.FromDate;
        var toDate = request.ToDate;

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Posts)}:{role}:{brandId}")}:{code}:{status}:{request.Page}" +
            $":{request.Size}:{sortBy}:{request.IsAsc}:{fromDate}:{toDate}";
    }
}