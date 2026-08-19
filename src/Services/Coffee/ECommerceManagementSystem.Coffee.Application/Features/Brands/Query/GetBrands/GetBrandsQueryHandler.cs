using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Brands;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrands;

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetBrandsQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IRedisService redisService, ILogger logger, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _logger = logger;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var cacheKey = BuildCacheKey(request);

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetBrandsResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách thương hiệu thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var brands = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .GetPagingListAsync<GetBrandsResponse>(
                predicate: x => ((string.IsNullOrEmpty(request.Code) || x.Name.Contains(request.Code))
                                 && (string.IsNullOrEmpty(request.Name) || x.Name.Contains(request.Name))
                                 && (request.Status == null || x.Status == request.Status)),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        foreach (var brand in brands.Items)
        {
            if (!string.IsNullOrWhiteSpace(brand.LogoPath))
            {
                try
                {
                    brand.LogoUrl = await _mediaService.GetImageUrlAsync(
                        brand.LogoUrl,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        brand.LogoUrl,
                        ex.Message
                    );
                }
            }
        }

        try
        {
            var serializedData = JsonSerializer.Serialize(brands);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.BrandsCacheTTL
            );
            _logger.Information($"Cached brands list with key: {cacheKey}, TTL: {CacheConfig.BrandsCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache brands list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách thương hiệu thành công",
            Data = brands
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: brands:list:{name}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetBrandsQuery request)
    {
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name;
        var sortBy = request.SortBy ?? "CreatedDate";

        return
            $"{CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))}:{name}:{request.Page}:{request.Size}:{sortBy}:" +
            $"{request.IsAsc}:{request.Code}:{request.Name}:{request.Status}";
    }
}