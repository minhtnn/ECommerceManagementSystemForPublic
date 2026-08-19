using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.CreateBrandPromotionRule;

public class CreateBrandPromotionRuleCommandHandler : IRequestHandler<CreateBrandPromotionRuleCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;

    public CreateBrandPromotionRuleCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation,
        ILogger logger,
        IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(
        CreateBrandPromotionRuleCommand request,
        CancellationToken cancellationToken)
    {
        #region Authorization

        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền thực hiện thao tác này!"
            };
        }

        #endregion

        #region Validate inputs

        if (request.StartDate.HasValue || request.EndDate.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.TimeZone))
            {
                return new ApiResponse
                {
                    Status  = StatusCodes.Status400BadRequest,
                    Message = "Vui lòng cung cấp TimeZone khi truyền StartDate/EndDate"
                };
            }
        }
        
        #region Convert timezone → UTC

        try
        {
            request.StartDate = TimeUtil.ConvertToUtc(request.StartDate, request.TimeZone);
            request.EndDate   = TimeUtil.ConvertToUtc(request.EndDate,   request.TimeZone);
        }
        catch (ArgumentException ex)
        {
            return new ApiResponse
            {
                Status  = StatusCodes.Status400BadRequest,
                Message = ex.Message // "Timezone '...' không hợp lệ"
            };
        }

        #endregion
        
        var now = DateTime.UtcNow;

        // ─── StartDate không được là quá khứ ────────────────────────
        if (request.StartDate.HasValue && request.StartDate.Value <= now)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Ngày bắt đầu không được là thời điểm trong quá khứ"
            };
        }
        
        

        // ─── FreeShipping không được có GlobalDiscountCap ────────────
        if (request.PromotionType == EPromotionType.FreeShipping && request.GlobalDiscountCap > 0)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "FreeShipping promotion không cần GlobalDiscountCap"
            };
        }

        // ─── Không được có 2 conditions cùng loại ───────────────────
        if (request.RuleConditions != null && request.RuleConditions.Any())
        {
            var duplicateConditionType = request.RuleConditions
                .GroupBy(c => c.ConditionType)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateConditionType != null)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = $"Không được có 2 điều kiện cùng loại '{duplicateConditionType.Key}' trong 1 khuyến mãi"
                };
            }
        }

        // ─── BuyXGetY: bắt buộc phải có condition MinQuantityOfProduct ─
        if (request.PromotionType == EPromotionType.BuyXGetY)
        {
            var hasMinQtyCondition = request.RuleConditions != null &&
                request.RuleConditions.Any(c => c.ConditionType == ERuleConditionType.MinQuantityOfProduct);

            if (!hasMinQtyCondition)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "BuyXGetY bắt buộc phải có điều kiện 'MinQuantityOfProduct' " +
                              "để xác định sản phẩm cần mua và số lượng tối thiểu (format: 'productId:minQty')"
                };
            }
        }

        // ─── Validate actions theo PromotionType ─────────────────────
        var validationError = ValidateActionsByPromotionType(request);
        if (validationError != null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = validationError
            };
        }

        // ─── Validate target IDs tồn tại trong DB (Action targets) ──
        if (request.RuleActions != null)
        {
            var allTargets = request.RuleActions
                .Where(a => a.RuleActionTargets != null)
                .SelectMany(a => a.RuleActionTargets!)
                .ToList();

            var productTargetIds = allTargets
                .Where(t => t.TargetType == EActionTargetType.Product)
                .Select(t => t.TargetId)
                .Distinct()
                .ToList();

            var categoryTargetIds = allTargets
                .Where(t => t.TargetType == EActionTargetType.Category)
                .Select(t => t.TargetId)
                .Distinct()
                .ToList();

            if (productTargetIds.Any())
            {
                var existingProducts = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                    .GetListAsync(predicate: x => productTargetIds.Contains(x.Id));

                if (existingProducts.Count != productTargetIds.Count)
                {
                    return new ApiResponse
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = "Một hoặc nhiều sản phẩm trong danh sách target không tồn tại"
                    };
                }
            }

            if (categoryTargetIds.Any())
            {
                var existingCategories = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                    .GetListAsync(predicate: x => categoryTargetIds.Contains(x.Id) && x.BrandId == brandId);

                if (existingCategories.Count != categoryTargetIds.Count)
                {
                    return new ApiResponse
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = "Một hoặc nhiều danh mục trong danh sách target không tồn tại " +
                                  "hoặc không thuộc thương hiệu này"
                    };
                }
            }
        }

        // ─── Validate target IDs trong conditions ───────────────────
        if (request.RuleConditions != null)
        {
            foreach (var condition in request.RuleConditions)
            {
                var conditionValidationError =
                    await ValidateConditionTargetExistsAsync(condition, brandId, cancellationToken);
                if (conditionValidationError != null)
                {
                    return new ApiResponse
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = conditionValidationError
                    };
                }
            }
        }

        // ─── Conflict check (warning only, không block) ──────────────
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var conflictingPromotions = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
                .GetListAsync(predicate: x =>
                    x.BrandId == brandId
                    && x.Status == EPromotionStatus.Active
                    && x.PromotionType == request.PromotionType
                    && x.StartDate < request.EndDate
                    && x.EndDate > request.StartDate);

            if (conflictingPromotions.Any())
            {
                _logger.Warning(
                    "Brand {BrandId} đang tạo promotion type '{Type}' có thể conflict với {Count} promotion(s) " +
                    "đang active trong cùng khoảng thời gian. Stacking engine sẽ tự xử lý chọn best promotion.",
                    brandId,
                    request.PromotionType,
                    conflictingPromotions.Count
                );
            }
        }

        #endregion

        #region Start transaction

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Không thể bắt đầu transaction: {Message}", transactionResult.Message);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        #endregion

        #region Create entities

        var promotionRule = new Domain.Entities.PromotionRules
        {
            Id = Guid.CreateVersion7(),
            BrandId = brandId,
            Code = request.Code,
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            PromotionType = request.PromotionType,
            GlobalDiscountCap = request.GlobalDiscountCap > 0 ? request.GlobalDiscountCap : null,
            Priority = request.Priority > 0
                ? request.Priority
                : GetDefaultPriorityByType(request.PromotionType),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = EPromotionStatus.Pending,
            CreatedDate = now,
            LastModifiedDate = now
        };

        await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>().InsertAsync(promotionRule);

        if (request.RuleConditions != null && request.RuleConditions.Any())
        {
            var conditions = request.RuleConditions.Select(c => new RuleConditions
            {
                Id = Guid.CreateVersion7(),
                PromotionRuleId = promotionRule.Id,
                ConditionType = c.ConditionType,
                Operator = c.Operator,
                Value = c.Value
            }).ToList();

            await _unitOfWork.GetRepository<RuleConditions>().InsertRangeAsync(conditions);

            _logger.Information(
                "Tạo {Count} conditions cho promotion {PromotionId}",
                conditions.Count,
                promotionRule.Id
            );
        }

        if (request.RuleActions != null && request.RuleActions.Any())
        {
            foreach (var actionRequest in request.RuleActions)
            {
                var action = new RuleActions
                {
                    Id = Guid.CreateVersion7(),
                    PromotionRuleId = promotionRule.Id,
                    ActionType = actionRequest.ActionType,
                    Value = actionRequest.ActionType is ERuleActionType.BuyXGetYFreeProducts
                                or ERuleActionType.FreeGiftProduct
                                or ERuleActionType.FreeShipping
                        ? null
                        : actionRequest.Value,
                    MaxDiscountAmountForPercentage = actionRequest.MaxDiscountAmountForPercentage > 0
                        ? actionRequest.MaxDiscountAmountForPercentage
                        : null
                };

                await _unitOfWork.GetRepository<RuleActions>().InsertAsync(action);

                if (actionRequest.RuleActionTargets != null && actionRequest.RuleActionTargets.Any())
                {
                    var targets = actionRequest.RuleActionTargets.Select(t => new RuleActionTargets
                    {
                        Id = Guid.CreateVersion7(),
                        RuleActionId = action.Id,
                        TargetType = t.TargetType,
                        TargetId = t.TargetId,
                        Quantity = t.Quantity > 0 ? t.Quantity : 1,
                        Role = t.Role
                    }).ToList();

                    await _unitOfWork.GetRepository<RuleActionTargets>().InsertRangeAsync(targets);
                }
            }

            _logger.Information(
                "Tạo {Count} actions cho promotion {PromotionId}",
                request.RuleActions.Count,
                promotionRule.Id
            );
        }

        #endregion

        #region Commit Transaction

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed khi tạo promotion: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            throw new Exception($"Không thể tạo khuyến mãi: {commitResult.Message}");
        }

        #endregion

        #region Invalidate Cache

        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
            ),
            entityCachePrefix: CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
        );

        if (cacheResult.Success)
        {
            _logger.Information(
                "Tạo promotion '{Name}' (ID: {Id}) thành công. Cache invalidated.",
                promotionRule.Name,
                promotionRule.Id
            );
        }
        else
        {
            _logger.Warning(
                "Tạo promotion '{Name}' thành công nhưng cache invalidation có vấn đề: {Message}",
                promotionRule.Name,
                cacheResult.Message
            );
        }

        #endregion

        return new ApiResponse
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo khuyến mãi thành công",
            Data = promotionRule.Id
        };
    }

    // =========================================================
    // VALIDATE ACTIONS THEO PROMOTION TYPE
    // =========================================================

    private static string? ValidateActionsByPromotionType(CreateBrandPromotionRuleCommand request)
    {
        var actions = request.RuleActions;
        if (actions == null || !actions.Any())
            return "Khuyến mãi phải có ít nhất 1 action";

        return request.PromotionType switch
        {
            EPromotionType.OrderDiscount    => ValidateOrderDiscountActions(actions),
            EPromotionType.LineItemDiscount => ValidateLineItemDiscountActions(actions),
            EPromotionType.BuyXGetY         => ValidateBuyXGetYActions(actions),
            // EPromotionType.QuantityTier     => ValidateQuantityTierActions(actions),
            EPromotionType.FreeGift         => ValidateFreeGiftActions(actions),
            EPromotionType.FreeShipping     => ValidateFreeShippingActions(actions),
            _                               => "PromotionType không hợp lệ"
        };
    }

    private static string? ValidateOrderDiscountActions(List<CreateBrandRuleAction> actions)
    {
        var allowedTypes = new[]
        {
            ERuleActionType.CartPercentageDiscount,
            ERuleActionType.CartFixedDiscount
        };

        var invalid = actions.FirstOrDefault(a => !allowedTypes.Contains(a.ActionType));
        if (invalid != null)
            return $"OrderDiscount chỉ được dùng 'CartPercentageDiscount' hoặc 'CartFixedDiscount'. " +
                   $"Action '{invalid.ActionType}' không hợp lệ.";

        foreach (var action in actions)
        {
            if (action.ActionType == ERuleActionType.CartPercentageDiscount)
                if (!decimal.TryParse(action.Value, out var pct) || pct <= 0 || pct > 100)
                    return "Phần trăm giảm giá phải là số trong khoảng (0, 100]";

            if (action.ActionType == ERuleActionType.CartFixedDiscount)
                if (!decimal.TryParse(action.Value, out var amount) || amount <= 0)
                    return "Số tiền giảm cố định phải là số dương";
        }

        return null;
    }

    private static string? ValidateLineItemDiscountActions(List<CreateBrandRuleAction> actions)
    {
        var allowedTypes = new[]
        {
            ERuleActionType.ItemPercentageDiscount,
            ERuleActionType.ItemFixedDiscount
        };

        var invalid = actions.FirstOrDefault(a => !allowedTypes.Contains(a.ActionType));
        if (invalid != null)
            return $"LineItemDiscount chỉ được dùng 'ItemPercentageDiscount' hoặc 'ItemFixedDiscount'. " +
                   $"Action '{invalid.ActionType}' không hợp lệ.";

        foreach (var action in actions)
        {
            if (action.ActionType == ERuleActionType.ItemPercentageDiscount)
                if (!decimal.TryParse(action.Value, out var pct) || pct <= 0 || pct > 100)
                    return "Phần trăm giảm giá item phải là số trong khoảng (0, 100]";

            if (action.ActionType == ERuleActionType.ItemFixedDiscount)
                if (!decimal.TryParse(action.Value, out var amount) || amount <= 0)
                    return "Số tiền giảm cố định per item phải là số dương";

            var hasDiscountTarget = action.RuleActionTargets != null &&
                                    action.RuleActionTargets.Any(t => t.Role == EActionTargetRole.DiscountTarget);
            if (!hasDiscountTarget)
                return "Mỗi action của LineItemDiscount phải có ít nhất 1 target với role 'DiscountTarget'";
        }

        var allDiscountTargetIds = actions
            .Where(a => a.RuleActionTargets != null)
            .SelectMany(a => a.RuleActionTargets!.Where(t => t.Role == EActionTargetRole.DiscountTarget))
            .Select(t => t.TargetId)
            .ToList();

        if (allDiscountTargetIds.Count != allDiscountTargetIds.Distinct().Count())
            return "Không được có sản phẩm/danh mục trùng lặp giữa các actions trong cùng 1 khuyến mãi";

        return null;
    }

    /// <summary>
    /// BuyXGetY KHÔNG dùng BuyProduct target.
    /// Thông tin "mua bao nhiêu sản phẩm nào" đã nằm trong condition MinQuantityOfProduct.
    /// Action chỉ cần chứa GetProduct targets (sản phẩm được tặng).
    /// </summary>
    private static string? ValidateBuyXGetYActions(List<CreateBrandRuleAction> actions)
    {
        if (actions.Count != 1)
            return "BuyXGetY chỉ được có đúng 1 action";

        var action = actions[0];

        if (action.ActionType != ERuleActionType.BuyXGetYFreeProducts)
            return "BuyXGetY phải dùng action 'BuyXGetYFreeProducts'";

        // Không được có BuyProduct target
        var hasBuyProductTarget = action.RuleActionTargets != null &&
                                  action.RuleActionTargets.Any(t => t.Role == EActionTargetRole.BuyProduct);
        if (hasBuyProductTarget)
            return "BuyXGetY không sử dụng target 'BuyProduct'. " +
                   "Sản phẩm cần mua và số lượng phải khai báo trong điều kiện 'MinQuantityOfProduct' (format: 'productId:minQty')";

        // Phải có ít nhất 1 GetProduct target
        var getTargets = action.RuleActionTargets?
            .Where(t => t.Role == EActionTargetRole.GetProduct)
            .ToList();

        if (getTargets == null || !getTargets.Any())
            return "BuyXGetY phải có ít nhất 1 target với role 'GetProduct' (sản phẩm được tặng)";

        // GetProduct chỉ được là Product, không được là Category
        if (getTargets.Any(t => t.TargetType != EActionTargetType.Product))
            return "GetProduct target của BuyXGetY phải là Product, không được dùng Category";

        // Số lượng tặng phải >= 1
        if (getTargets.Any(t => t.Quantity < 1))
            return "Số lượng sản phẩm được tặng (GetProduct.Quantity) phải >= 1";

        return null;
    }

    private static string? ValidateQuantityTierActions(List<CreateBrandRuleAction> actions)
    {
        var allowedTypes = new[]
        {
            ERuleActionType.CartPercentageDiscount,
            ERuleActionType.CartFixedDiscount,
            ERuleActionType.ItemPercentageDiscount,
            ERuleActionType.ItemFixedDiscount
        };

        var invalid = actions.FirstOrDefault(a => !allowedTypes.Contains(a.ActionType));
        if (invalid != null)
            return $"QuantityTier không hỗ trợ action '{invalid.ActionType}'. " +
                   "Chỉ được dùng: CartPercentageDiscount, CartFixedDiscount, ItemPercentageDiscount, ItemFixedDiscount";

        foreach (var action in actions)
        {
            if (!decimal.TryParse(action.Value, out var val) || val <= 0)
                return "Giá trị discount của QuantityTier phải là số dương";

            if (action.ActionType is ERuleActionType.CartPercentageDiscount
                                  or ERuleActionType.ItemPercentageDiscount && val > 100)
                return "Phần trăm giảm giá phải <= 100";

            // Item-level actions phải có DiscountTarget
            if (action.ActionType is ERuleActionType.ItemPercentageDiscount
                                  or ERuleActionType.ItemFixedDiscount)
            {
                var hasTarget = action.RuleActionTargets != null &&
                                action.RuleActionTargets.Any(t => t.Role == EActionTargetRole.DiscountTarget);
                if (!hasTarget)
                    return $"Action '{action.ActionType}' trong QuantityTier phải có ít nhất 1 DiscountTarget";
            }
        }

        return null;
    }

    private static string? ValidateFreeGiftActions(List<CreateBrandRuleAction> actions)
    {
        if (actions.Count != 1)
            return "FreeGift chỉ được có đúng 1 action";

        var action = actions[0];

        if (action.ActionType != ERuleActionType.FreeGiftProduct)
            return "FreeGift phải dùng action 'FreeGiftProduct'";

        var giftTargets = action.RuleActionTargets?
            .Where(t => t.Role == EActionTargetRole.GiftProduct)
            .ToList();

        if (giftTargets == null || giftTargets.Count != 1)
            return "FreeGift phải có đúng 1 target với role 'GiftProduct'";

        if (giftTargets[0].TargetType != EActionTargetType.Product)
            return "GiftProduct target phải là Product";

        if (giftTargets[0].Quantity < 1)
            return "Số lượng quà tặng phải >= 1";

        return null;
    }

    private static string? ValidateFreeShippingActions(List<CreateBrandRuleAction> actions)
    {
        if (actions.Count != 1)
            return "FreeShipping chỉ được có đúng 1 action";

        if (actions[0].ActionType != ERuleActionType.FreeShipping)
            return "FreeShipping promotion chỉ được dùng action 'FreeShipping'";

        // FreeShipping không cần target
        if (actions[0].RuleActionTargets != null && actions[0].RuleActionTargets.Any())
            return "FreeShipping action không cần target";

        return null;
    }

    // =========================================================
    // VALIDATE CONDITION TARGETS TỒN TẠI TRONG DB
    // =========================================================

    private async Task<string?> ValidateConditionTargetExistsAsync(
        CreateBrandRuleCondition condition,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        switch (condition.ConditionType)
        {
            case ERuleConditionType.CartContainsProduct:
            {
                if (string.IsNullOrWhiteSpace(condition.Value)) break;
                var ids = ParseGuidList(condition.Value);
                if (ids == null)
                    return "CartContainsProduct value phải là danh sách GUID hợp lệ, phân cách bởi dấu phẩy";

                var existing = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                    .GetListAsync(predicate: x => ids.Contains(x.Id));
                if (existing.Count != ids.Count)
                    return "Một hoặc nhiều sản phẩm trong điều kiện CartContainsProduct không tồn tại";
                break;
            }

            case ERuleConditionType.CartContainsCategory:
            {
                if (string.IsNullOrWhiteSpace(condition.Value)) break;
                var ids = ParseGuidList(condition.Value);
                if (ids == null)
                    return "CartContainsCategory value phải là danh sách GUID hợp lệ";

                var existing = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                    .GetListAsync(predicate: x => ids.Contains(x.Id) && x.BrandId == brandId);
                if (existing.Count != ids.Count)
                    return "Một hoặc nhiều danh mục trong điều kiện CartContainsCategory không tồn tại " +
                           "hoặc không thuộc thương hiệu này";
                break;
            }

            case ERuleConditionType.MinQuantityOfProduct:
            {
                if (string.IsNullOrWhiteSpace(condition.Value)) break;
                var parsed = ParseIdColonQty(condition.Value);
                if (parsed == null)
                    return "MinQuantityOfProduct value phải theo format 'productId:minQty' (VD: 'uuid:3')";

                var exists = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                    .SingleOrDefaultAsync(predicate: x => x.Id == parsed.Value.Id);
                if (exists == null)
                    return $"Sản phẩm '{parsed.Value.Id}' trong điều kiện MinQuantityOfProduct không tồn tại";
                break;
            }

            case ERuleConditionType.MinQuantityInCategory:
            {
                if (string.IsNullOrWhiteSpace(condition.Value)) break;
                var parsed = ParseIdColonQty(condition.Value);
                if (parsed == null)
                    return "MinQuantityInCategory value phải theo format 'categoryId:minQty' (VD: 'uuid:5')";

                var exists = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                    .SingleOrDefaultAsync(predicate: x => x.Id == parsed.Value.Id && x.BrandId == brandId);
                if (exists == null)
                    return $"Danh mục '{parsed.Value.Id}' trong điều kiện MinQuantityInCategory không tồn tại";
                break;
            }
        }

        return null;
    }

    // =========================================================
    // UTILITY METHODS
    // =========================================================

    private static int GetDefaultPriorityByType(EPromotionType type) => type switch
    {
        EPromotionType.OrderDiscount    => 50,
        EPromotionType.LineItemDiscount => 40,
        EPromotionType.BuyXGetY         => 30,
        // EPromotionType.QuantityTier     => 20,
        EPromotionType.FreeGift         => 10,
        EPromotionType.FreeShipping     => 0,
        _                               => 0
    };

    private static List<Guid>? ParseGuidList(string value)
    {
        try
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.Parse(s.Trim()))
                .ToList();
        }
        catch { return null; }
    }

    private static (Guid Id, int Qty)? ParseIdColonQty(string value)
    {
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