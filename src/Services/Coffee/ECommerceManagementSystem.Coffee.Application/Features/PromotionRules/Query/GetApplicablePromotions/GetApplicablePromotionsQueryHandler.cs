using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetApplicablePromotions;

public class GetApplicablePromotionsQueryHandler
    : IRequestHandler<GetApplicablePromotionsQuery, ApiResponse>
{
    private readonly ILogger _logger;
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
 
    private const string MetadataField = "metadata";
 
    public GetApplicablePromotionsQueryHandler(
        ILogger logger,
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IRedisService redisService,
        IClaimService claimService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _claimService = claimService;
    }
 
    public async ValueTask<ApiResponse> Handle(
        GetApplicablePromotionsQuery request,
        CancellationToken cancellationToken)
    {
        // ─── STEP 1: Auth ────────────────────────────────────────────
        var role = _claimService.GetCurrentRoleEnum();
        if (role != ERole.EndCustomer)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Chỉ khách hàng mới có thể xem khuyến mãi áp dụng được!"
            };
        }
 
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code == request.BrandCode && x.Status == EBrandStatus.Active
            );
        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Không tìm thấy thương hiệu nào!");
        }
        
        var customerId = _claimService.GetCurrentReferenceId();
        if (customerId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Thông tin khách hàng không hợp lệ!"
            };
        }
 
        // ─── STEP 2: Load cart từ Redis ──────────────────────────────
        var cart = await GetActiveCartAsync(customerId);
        if (cart == null || !cart.Items.Any(i => !i.IsGiftItem))
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Giỏ hàng trống",
                Data = new List<GetApplicablePromotionRulesResponse>()
            };
        }
 
        // ─── STEP 3: Lấy BrandId từ sản phẩm đầu tiên trong giỏ ────
        var firstProductId = cart.Items
            .Where(i => !i.IsGiftItem)
            .Select(i => i.ProductId)
            .First();
 
        var firstProduct = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .SingleOrDefaultAsync(predicate: x => x.Id == firstProductId);
 
        if (firstProduct == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Data = new List<GetApplicablePromotionRulesResponse>()
            };
        }
 
        var category = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .SingleOrDefaultAsync(predicate: x => x.Id == firstProduct.ProductCategoryId);
 
        if (category == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Data = new List<GetApplicablePromotionRulesResponse>()
            };
        }
 
        // ─── STEP 4: Load promotions Active của Brand ────────────────
        var now = DateTime.UtcNow;
 
        var allActivePromotions = await _unitOfWork
            .GetRepository<Domain.Entities.PromotionRules>()
            .GetListAsync(
                predicate: x =>
                    x.BrandId == category.BrandId
                    && x.Status == EPromotionStatus.Active
                    && x.StartDate <= now
                    && x.EndDate >= now,
                include: q => q
                    .Include(p => p.RuleConditions)
                    .Include(p => p.RuleActions)
                    .ThenInclude(a => a.RuleActionTargets)
            );
 
        if (!allActivePromotions.Any())
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Data = new List<GetApplicablePromotionRulesResponse>()
            };
        }
 
        // ─── STEP 5: Lọc bỏ promotions đã apply ─────────────────────
        var alreadyAppliedIds = cart.AppliedPromotions
            .Select(p => p.PromotionId)
            .ToHashSet();
 
        var candidates = allActivePromotions
            .Where(p => !alreadyAppliedIds.Contains(p.Id))
            .ToList();
 
        // ─── STEP 6: Evaluate từng promotion ────────────────────────
        var result = new List<GetApplicablePromotionRulesResponse>();
 
        foreach (var promotion in candidates)
        {
            var (conditionsMet, _) = await EvaluateConditionsAsync(
                promotion.RuleConditions, cart, customerId);
 
            if (!conditionsMet) continue;
 
            result.Add(new GetApplicablePromotionRulesResponse
            {
                Id = promotion.Id,
                Code = promotion.Code,
                Description = promotion.Description,
                Name = promotion.Name,
                ShortDescription = promotion.ShortDescription,
            });
        }
 
        _logger.Information(
            "GetApplicablePromotions: customer {CustomerId} — {Count}/{Total} promotions qualified",
            customerId, result.Count, candidates.Count);
 
        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = $"Tìm thấy {result.Count} khuyến mãi áp dụng được",
            Data = result
        };
    }
 
    // =========================================================
    // EVALUATE CONDITIONS
    // (copy từ UpdateCartCommandHandler, tách private để không phụ thuộc)
    // =========================================================
 
    private async Task<(bool Met, string? Error)> EvaluateConditionsAsync(
        ICollection<RuleConditions> conditions,
        GetCustomerCartResponse cart,
        Guid customerId)
    {
        var nonGiftItems = cart.Items.Where(i => !i.IsGiftItem).ToList();
        var cartSubtotal = nonGiftItems.Sum(i => i.TotalAmountSnapshot);
        var cartProductIds = nonGiftItems.Select(i => i.ProductId).ToHashSet();
        var cartTotalQty = nonGiftItems.Sum(i => i.Quantity);
 
        foreach (var condition in conditions)
        {
            switch (condition.ConditionType)
            {
                case ERuleConditionType.CartSubtotal:
                {
                    if (!decimal.TryParse(condition.Value, out var threshold))
                        return (false, null);
 
                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.GreaterThanOrEqual => cartSubtotal >= threshold,
                        ERuleConditionOperator.GreaterThan         => cartSubtotal > threshold,
                        ERuleConditionOperator.Equals              => cartSubtotal == threshold,
                        _ => false
                    };
 
                    if (!passed) return (false, null);
                    break;
                }
 
                case ERuleConditionType.CartContainsProduct:
                {
                    var ids = ParseGuidList(condition.Value);
                    if (ids == null) return (false, null);
 
                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.ContainsAny => ids.Any(id => cartProductIds.Contains(id)),
                        ERuleConditionOperator.ContainsAll => ids.All(id => cartProductIds.Contains(id)),
                        _ => false
                    };
 
                    if (!passed) return (false, null);
                    break;
                }
 
                case ERuleConditionType.TotalCartQuantity:
                {
                    if (!int.TryParse(condition.Value, out var minQty))
                        return (false, null);
 
                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.GreaterThanOrEqual => cartTotalQty >= minQty,
                        ERuleConditionOperator.GreaterThan         => cartTotalQty > minQty,
                        ERuleConditionOperator.Equals              => cartTotalQty == minQty,
                        _ => false
                    };
 
                    if (!passed) return (false, null);
                    break;
                }
 
                case ERuleConditionType.MinQuantityOfProduct:
                {
                    var parsed = ParseIdColonQty(condition.Value);
                    if (parsed == null) return (false, null);
 
                    var (productId, minQty) = parsed.Value;
                    var itemQty = nonGiftItems
                        .Where(i => i.ProductId == productId)
                        .Sum(i => i.Quantity);
 
                    if (itemQty < minQty) return (false, null);
                    break;
                }
 
                case ERuleConditionType.FirstOrder:
                {
                    var hasOrder = await _unitOfWork
                        .GetRepository<Domain.Entities.Orders>()
                        .AnyAsync(predicate: x =>
                            x.CustomerId == customerId
                            && x.OrderStatus != EOrderStatus.Cancelled);
 
                    if (hasOrder) return (false, null);
                    break;
                }
 
                case ERuleConditionType.CartContainsCategory:
                case ERuleConditionType.MinQuantityInCategory:
                    // Cần join DB — skip ở cart level
                    break;
            }
        }
 
        return (true, null);
    }
 
    // =========================================================
    // REDIS HELPERS
    // =========================================================
 
    private async Task<GetCustomerCartResponse?> GetActiveCartAsync(Guid customerId)
    {
        var hashKey = $"{CacheConfig.EntityListCachePrefix("carts")}:{customerId}";
 
        var metadataJson = await _redisService.GetHashAsync(hashKey, MetadataField);
        if (string.IsNullOrEmpty(metadataJson)) return null;
 
        var metadata = JsonSerializer.Deserialize<CartMetadata>(metadataJson);
        if (metadata?.ActiveCartId == null) return null;
 
        var cartJson = await _redisService.GetHashAsync(
            hashKey, $"cart:{metadata.ActiveCartId.Value}");
        if (string.IsNullOrEmpty(cartJson)) return null;
 
        return JsonSerializer.Deserialize<GetCustomerCartResponse>(cartJson);
    }
 
    // =========================================================
    // PARSE HELPERS
    // =========================================================
 
    private static List<Guid>? ParseGuidList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.Parse(s.Trim())).ToList();
        }
        catch { return null; }
    }
 
    private static (Guid Id, int Qty)? ParseIdColonQty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var parts = value.Split(':');
            if (parts.Length != 2) return null;
            if (!Guid.TryParse(parts[0].Trim(), out var id)) return null;
            if (!int.TryParse(parts[1].Trim(), out var qty) || qty < 1) return null;
            return (id, qty);
        }
        catch { return null; }
    }
}
 