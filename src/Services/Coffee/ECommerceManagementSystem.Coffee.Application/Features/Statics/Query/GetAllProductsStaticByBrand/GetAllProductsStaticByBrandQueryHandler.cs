using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Statistics;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Statics.Query.GetAllProductsStaticByBrand;

public class GetAllProductsStaticByBrandQueryHandler : IRequestHandler<GetAllProductsStaticByBrandQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetAllProductsStaticByBrandQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetAllProductsStaticByBrandQuery request,
        CancellationToken cancellationToken)
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

        var cacheKey = BuildCacheKey(request, role, brandId.ToString());
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult =
                    JsonSerializer.Deserialize<Paginate<GetAllProductsStaticByBrandResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thống kê sản phẩm thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var productsStatic = await _unitOfWork.GetRepository<DailyProductSales>()
            .GetPagingListAsync<GetAllProductsStaticByBrandResponse>(
                predicate: x => x.Product.ProductCategory.BrandId == brandId,
                include: x => x.Include(dp => dp.Product)
                    .ThenInclude(p => p.ProductCategory),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        foreach (var category in productsStatic.Items)
        {
            if (category.ProductImagePath != null && !string.IsNullOrEmpty(category.ProductImagePath))
            {
                try
                {
                    category.ProductImageUrl = await _mediaService.GetImageUrlAsync(
                        category.ProductImagePath,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        category.ProductImagePath,
                        ex.Message
                    );
                }
            }
        }

        try
        {
            var serializedData = JsonSerializer.Serialize(productsStatic);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.ProductsSaleStatisticCacheTTL
            );
            _logger.Information(
                $"Cached product sale statistic list with key: {cacheKey}, TTL: {CacheConfig.ProductCategoriesCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache product sale statistic list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thống kê sản phẩm thành công",
            Data = productsStatic
        };
    }

    private string BuildCacheKey(GetAllProductsStaticByBrandQuery request, ERole role, string brandId)
    {
        var sortBy = request.SortBy ?? "CreatedDate";
        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(DailyProductSales)}:{role}:{brandId}")}:{request.Page}" +
            $":{request.Size}:{sortBy}:{request.IsAsc}";
    }
}