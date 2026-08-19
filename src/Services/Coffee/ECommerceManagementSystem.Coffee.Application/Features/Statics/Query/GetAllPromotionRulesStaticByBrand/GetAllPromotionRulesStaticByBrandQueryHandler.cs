using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;
using ECommerceManagementSystem.Coffee.Domain.Models.Statistics;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Statics.Query.GetAllPromotionRulesStaticByBrand;

public class
    GetAllPromotionRulesStaticByBrandQueryHandler : IRequestHandler<GetAllPromotionRulesStaticByBrandQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;

    public GetAllPromotionRulesStaticByBrandQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetAllPromotionRulesStaticByBrandQuery request,
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
                    JsonSerializer.Deserialize<Paginate<GetAllPromotionRulesStaticByBrandResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thống kê khuyến mãi thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var promotionRulesStatic = await _unitOfWork.GetRepository<DailyPromotionStats>()
            .GetPagingListAsync<GetAllPromotionRulesStaticByBrandResponse>(
                predicate: x => x.PromotionRule.BrandId == brandId,
                include: x => x.Include(x => x.PromotionRule),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        
        try
        {
            var serializedData = JsonSerializer.Serialize(promotionRulesStatic);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PromotionRulesStatisticCacheTTL
            );
            _logger.Information(
                $"Cached promotion rule sale statistic list with key: {cacheKey}, TTL: {CacheConfig.ProductCategoriesCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache promotion rule sale statistic list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thống kê khuyến mãi thành công",
            Data = promotionRulesStatic
        };
    }
    
    private string BuildCacheKey(GetAllPromotionRulesStaticByBrandQuery request, ERole role, string brandId)
    {
        var sortBy = request.SortBy ?? "CreatedDate";
        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(DailyPromotionStats)}:{role}:{brandId}")}:{request.Page}" +
            $":{request.Size}:{sortBy}:{request.IsAsc}";
    }
}