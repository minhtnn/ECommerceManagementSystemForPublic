using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPromotionRuleById;

public class GetBrandPromotionRuleByIdQueryHandler : IRequestHandler<GetBrandPromotionRuleByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly ICacheInvalidationService _cacheService;

    public GetBrandPromotionRuleByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger, IClaimService claimService, ICacheInvalidationService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPromotionRuleByIdQuery request, CancellationToken cancellationToken)
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
        
        try
        {
            var cachedPromotionRule = await _cacheService.GetDetailFromCacheAsync<GetPromotionRuleByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.PromotionRules), request.Id.ToString())}:{role}:{brandId.ToString()}"
            );
            if (cachedPromotionRule != null)
            {
                _logger.Debug($"Cache HIT for product category:{request.Id}");
                cachedPromotionRule.CreatedDate = TimeUtil.ConvertFromUtc(cachedPromotionRule.CreatedDate, request.TimeZone);
                cachedPromotionRule.LastModifiedDate = cachedPromotionRule.LastModifiedDate.HasValue
                    ? TimeUtil.ConvertFromUtc(cachedPromotionRule.LastModifiedDate.Value, request.TimeZone)
                    : null;
                cachedPromotionRule.StartDate = TimeUtil.ConvertFromUtc(cachedPromotionRule.StartDate, request.TimeZone);
                cachedPromotionRule.EndDate = TimeUtil.ConvertFromUtc(cachedPromotionRule.EndDate, request.TimeZone);
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy khuyến mãi thành công",
                    Data = cachedPromotionRule
                };
            }

            _logger.Debug($"Cache MISS for promotion:{request.Id}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }
        
        var promotionRule = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
            .SingleOrDefaultAsync<GetPromotionRuleByIdResponse>(
                predicate: (x => x.Id == request.Id && x.BrandId == brandId)
            );
        if (promotionRule == null)
        {
            throw new BadHttpRequestException("Không tìm thấy khuyến mãi với ID đã cho");
        }
        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.PromotionRules), request.Id.ToString())}:{role}:{brandId.ToString()}",
                promotionRule, CacheConfig.PromotionRulesCacheTTL);
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache promotion {ProductId}: {Error}", promotionRule.Id, e.Message);
        }

        if (promotionRule != null)
        {
            promotionRule.CreatedDate = TimeUtil.ConvertFromUtc(promotionRule.CreatedDate, request.TimeZone);
            promotionRule.LastModifiedDate = promotionRule.LastModifiedDate.HasValue
                ? TimeUtil.ConvertFromUtc(promotionRule.LastModifiedDate.Value, request.TimeZone)
                : null;
            promotionRule.StartDate = TimeUtil.ConvertFromUtc(promotionRule.StartDate, request.TimeZone);
            promotionRule.EndDate = TimeUtil.ConvertFromUtc(promotionRule.EndDate, request.TimeZone);
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin khuyến mãi thành công",
            Data = promotionRule
        };
    }
}