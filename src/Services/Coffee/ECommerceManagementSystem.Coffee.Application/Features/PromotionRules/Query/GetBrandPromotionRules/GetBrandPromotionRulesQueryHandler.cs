using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPublicPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPromotionRule;

public class GetBrandPromotionRulesQueryHandler : IRequestHandler<GetBrandPromotionRulesQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly IRedisService _redisService;

    public GetBrandPromotionRulesQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IClaimService claimService, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
        _redisService = redisService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPromotionRulesQuery request,
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

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetPromotionRulesResponse>>(cachedData);
                if (cachedResult!= null && cachedResult.Items.Any())
                {
                    foreach (var x in cachedResult.Items)
                    {
                        x.StartDate = x.StartDate.HasValue? TimeUtil.ConvertFromUtc(x.StartDate.Value, request.TimeZone) : null;
                        x.EndDate =  x.EndDate.HasValue? TimeUtil.ConvertFromUtc(x.EndDate.Value, request.TimeZone) : null;
                    }
                    return new ApiResponse()
                    {
                        Status = StatusCodes.Status200OK,
                        Message = "Lấy danh sách khuyến mãi thành công.",
                        Data = cachedResult
                    };
                }
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var promotionRules = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
            .GetPagingListAsync<GetPromotionRulesResponse>(
                predicate: x => x.BrandId == brandId
                                          && (string.IsNullOrWhiteSpace(request.Code) || x.Code.Contains(request.Code))
                                          && (string.IsNullOrWhiteSpace(request.Name) || x.Code.Contains(request.Name))
                                          && (request.Status == null || x.Status == request.Status),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        #region Cache result

        try
        {
            var serializedData = JsonSerializer.Serialize(promotionRules);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PromotionRulesCacheTTL
            );
            _logger.Information(
                "Cached promotion rules list with key: {CacheKey}, TTL: {TTL}",
                cacheKey,
                CacheConfig.PromotionRulesCacheTTL
            );
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache products list: {Error}", redisEx.Message);
        }

        #endregion

        if (promotionRules != null && promotionRules.Items.Any())
        {
            foreach (var x in promotionRules.Items)
            {
                x.StartDate = x.StartDate.HasValue? TimeUtil.ConvertFromUtc(x.StartDate.Value, request.TimeZone) : null;
                x.EndDate =  x.EndDate.HasValue? TimeUtil.ConvertFromUtc(x.EndDate.Value, request.TimeZone) : null;
            }
        }
        
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách khuyến mãi thành công",
            Data = promotionRules
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: products:list:{name}:{status}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetBrandPromotionRulesQuery request, ERole role, string brandId)
    {
        var code = string.IsNullOrEmpty(request.Code) ? "all" : request.Code;
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name;
        var status = request.Status?.ToString() ?? "all";
        var sortBy = request.SortBy ?? "CreatedDate";

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")}:{code}:{name}:{status}:{request.Page}:{request.Size}:{sortBy}:{request.IsAsc}";
    }
}