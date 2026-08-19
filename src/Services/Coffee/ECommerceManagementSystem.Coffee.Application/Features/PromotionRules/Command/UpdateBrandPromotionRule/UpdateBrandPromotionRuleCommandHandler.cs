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
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.UpdateBrandPromotionRule;

public class UpdateBrandPromotionRuleCommandHandler : IRequestHandler<UpdateBrandPromotionRuleCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;

    public UpdateBrandPromotionRuleCommandHandler(
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
        UpdateBrandPromotionRuleCommand request,
        CancellationToken cancellationToken)
    {
        // ─── Auth ────────────────────────────────────────────────────
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

        // ─── Load Promotion ──────────────────────────────────────────
        var promotion = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.Id && x.BrandId == brandId,
                include: q => q
                    .Include(p => p.RuleConditions)
                    .Include(p => p.RuleActions)
                    .ThenInclude(a => a.RuleActionTargets)
            );

        if (promotion == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy khuyến mãi hoặc bạn không có quyền chỉnh sửa"
            };
        }

        // ─── Xác định PromotionLifecycleState ───────────────────────
        if (request.StartDate.HasValue || request.EndDate.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.TimeZone))
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Vui lòng cung cấp TimeZone khi truyền StartDate/EndDate"
                };
            }
        }

        #region Convert timezone → UTC

        try
        {
            request.StartDate = TimeUtil.ConvertToUtc(request.StartDate, request.TimeZone);
            request.EndDate = TimeUtil.ConvertToUtc(request.EndDate, request.TimeZone);
        }
        catch (ArgumentException ex)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = ex.Message // "Timezone '...' không hợp lệ"
            };
        }

        #endregion

        var now = DateTime.UtcNow;
        var lifecycleState = DetermineLifecycleState(promotion, now);

        _logger.Information(
            "Update promotion {Id} — LifecycleState: {State}",
            promotion.Id,
            lifecycleState
        );

        // ─── Guard: không cho sửa nếu đã hết hoặc đã tắt ───────────
        switch (lifecycleState)
        {
            case PromotionLifecycleState.Expired:
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Không thể chỉnh sửa khuyến mãi đã hết hạn"
                };

            // case PromotionLifecycleState.Inactive:
            //     return new ApiResponse
            //     {
            //         Status = StatusCodes.Status400BadRequest,
            //         Message = "Không thể chỉnh sửa khuyến mãi đã bị tắt. " +
            //                   "Vui lòng sử dụng chức năng Duplicate để tạo khuyến mãi mới từ cấu hình này"
            //     };
        }

        // ─── Route sang đúng flow ─────────────────────────────────────
        return lifecycleState switch
        {
            PromotionLifecycleState.NotStarted =>
                await HandleNotStartedUpdateAsync(promotion, request, brandId, role, now, cancellationToken),
            PromotionLifecycleState.Running =>
                await HandleRunningUpdateAsync(promotion, request, role, brandId, now, cancellationToken),
            PromotionLifecycleState.Inactive =>
                await HandleRunningUpdateAsync(promotion, request, role, brandId, now, cancellationToken),
            _ => new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Trạng thái khuyến mãi không hợp lệ"
            }
        };
    }

    // =========================================================
    // FLOW A: Chưa bắt đầu — cho sửa toàn bộ
    // =========================================================

    private async Task<ApiResponse> HandleNotStartedUpdateAsync(
        Domain.Entities.PromotionRules promotion,
        UpdateBrandPromotionRuleCommand request,
        Guid brandId,
        ERole? role,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // ─── Validate StartDate mới ──────────────────────────────────
        if (request.StartDate.HasValue && request.StartDate.Value <= now)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Ngày bắt đầu mới không được là thời điểm trong quá khứ"
            };
        }

        var effectiveStartDate = request.StartDate ?? promotion.StartDate;
        var effectiveEndDate = request.EndDate ?? promotion.EndDate;

        if (effectiveStartDate.HasValue && effectiveEndDate.HasValue
                                        && effectiveStartDate >= effectiveEndDate)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Ngày bắt đầu phải trước ngày kết thúc"
            };
        }

        var effectiveType = request.PromotionType ?? promotion.PromotionType;

        // ─── FreeShipping không được có GlobalDiscountCap ────────────
        if (effectiveType == EPromotionType.FreeShipping)
        {
            var effectiveCap = request.GlobalDiscountCap ?? promotion.GlobalDiscountCap;
            if (effectiveCap.HasValue && effectiveCap.Value > 0)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "FreeShipping promotion không cần GlobalDiscountCap"
                };
            }
        }

        // ─── Validate Conditions mới nếu có ─────────────────────────
        if (request.RuleConditions != null)
        {
            var conditionError = ValidateConditionsBasic(request.RuleConditions
                .Select(c => new ConditionDto
                {
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    Value = c.Value
                }).ToList());
            if (conditionError != null)
                return new ApiResponse { Status = StatusCodes.Status400BadRequest, Message = conditionError };

            var conditionDbError = await ValidateConditionTargetsExistAsync(
                request.RuleConditions.Select(c => new ConditionDto
                {
                    ConditionType = c.ConditionType,
                    Value = c.Value
                }).ToList(), brandId);
            if (conditionDbError != null)
                return new ApiResponse { Status = StatusCodes.Status400BadRequest, Message = conditionDbError };
        }

        // ─── BuyXGetY: phải có MinQuantityOfProduct condition ────────
        if (effectiveType == EPromotionType.BuyXGetY && request.RuleConditions != null)
        {
            var hasMinQty = request.RuleConditions
                .Any(c => c.ConditionType == ERuleConditionType.MinQuantityOfProduct);
            if (!hasMinQty)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "BuyXGetY bắt buộc phải có điều kiện 'MinQuantityOfProduct'"
                };
            }
        }

        // ─── Validate Actions mới nếu có ────────────────────────────
        if (request.RuleActions != null)
        {
            var actionDtos = request.RuleActions.Select(a => new ActionDto
            {
                ActionType = a.ActionType,
                Value = a.Value,
                MaxDiscountAmountForPercentage = a.MaxDiscountAmountForPercentage,
                Targets = a.RuleActionTargets?.Select(t => new TargetDto
                {
                    TargetType = t.TargetType,
                    TargetId = t.TargetId,
                    Quantity = t.Quantity,
                    Role = t.Role
                }).ToList()
            }).ToList();

            var actionError = ValidateActionsByPromotionType(effectiveType, actionDtos);
            if (actionError != null)
                return new ApiResponse { Status = StatusCodes.Status400BadRequest, Message = actionError };

            var actionDbError = await ValidateActionTargetsExistAsync(
                actionDtos.Where(a => a.Targets != null)
                    .SelectMany(a => a.Targets!)
                    .ToList(), brandId);
            if (actionDbError != null)
                return new ApiResponse { Status = StatusCodes.Status400BadRequest, Message = actionDbError };
        }

        // ─── Conflict check (warning only) ───────────────────────────
        if (effectiveStartDate.HasValue && effectiveEndDate.HasValue)
        {
            var conflicting = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
                .GetListAsync(predicate: x =>
                    x.BrandId == brandId
                    && x.Id != promotion.Id
                    && x.Status == EPromotionStatus.Active
                    && x.PromotionType == effectiveType
                    && x.StartDate < effectiveEndDate
                    && x.EndDate > effectiveStartDate);

            if (conflicting.Any())
                _logger.Warning(
                    "Promotion {Id} sau update có thể conflict với {Count} promotion(s) cùng type đang active",
                    promotion.Id, conflicting.Count);
        }

        // ─── Begin Transaction ───────────────────────────────────────
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

        var isStatusChage = request.Status.HasValue && (request.Status.Value != promotion.Status);
        // ─── Apply changes ───────────────────────────────────────────
        if (request.Name != null) promotion.Name = request.Name;
        if (request.ShortDescription != null) promotion.ShortDescription = request.ShortDescription;
        if (request.Description != null) promotion.Description = request.Description;
        if (request.Status.HasValue) promotion.Status = request.Status.Value;
        if (request.StartDate.HasValue) promotion.StartDate = request.StartDate;
        if (request.EndDate.HasValue) promotion.EndDate = request.EndDate;
        if (request.PromotionType.HasValue) promotion.PromotionType = request.PromotionType.Value;
        if (request.Priority.HasValue) promotion.Priority = request.Priority.Value;

        // GlobalDiscountCap: 0 = xóa cap (set null), > 0 = set cap mới, null = không đổi
        if (request.GlobalDiscountCap.HasValue)
        {
            promotion.GlobalDiscountCap = request.GlobalDiscountCap.Value == 0
                ? null
                : request.GlobalDiscountCap.Value;
        }

        promotion.LastModifiedDate = now;
        _unitOfWork.GetRepository<Domain.Entities.PromotionRules>().UpdateAsync(promotion);

        // ─── Replace All: Conditions ─────────────────────────────────
        if (request.RuleConditions != null)
        {
            await ReplaceConditionsAsync(promotion.Id, request.RuleConditions
                .Select(c => new ConditionDto
                {
                    ConditionType = c.ConditionType,
                    Operator = c.Operator,
                    Value = c.Value
                }).ToList());
        }

        // ─── Replace All: Actions + Targets ─────────────────────────
        if (request.RuleActions != null)
        {
            await ReplaceActionsAsync(promotion.Id, request.RuleActions
                .Select(a => new ActionDto
                {
                    ActionType = a.ActionType,
                    Value = a.Value,
                    MaxDiscountAmountForPercentage = a.MaxDiscountAmountForPercentage,
                    Targets = a.RuleActionTargets?.Select(t => new TargetDto
                    {
                        TargetType = t.TargetType,
                        TargetId = t.TargetId,
                        Quantity = t.Quantity,
                        Role = t.Role
                    }).ToList()
                }).ToList());
        }

        return await CommitAndInvalidateCacheAsync(promotion, role, brandId, isStatusChage,
            "Cập nhật khuyến mãi thành công");
    }

    // =========================================================
    // FLOW B: Đang chạy — chỉ sửa subset field
    // =========================================================

    private async Task<ApiResponse> HandleRunningUpdateAsync(
        Domain.Entities.PromotionRules promotion,
        UpdateBrandPromotionRuleCommand request,
        ERole? role,
        Guid brandId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // ─── Validate EndDate ────────────────────────────────────────
        if (request.EndDate.HasValue)
        {
            if (request.EndDate.Value <= now)
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Ngày kết thúc mới phải sau thời điểm hiện tại"
                };

            if (promotion.EndDate.HasValue && request.EndDate.Value < promotion.EndDate.Value)
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Khuyến mãi đang chạy chỉ được kéo dài thêm ngày kết thúc, không được rút ngắn"
                };
        }

        // ─── Validate Status ─────────────────────────────────────────
        if (request.Status.HasValue)
        {
            if ((promotion.Status != EPromotionStatus.Active && promotion.Status != EPromotionStatus.Inactive) ||
               ( request.Status.Value != EPromotionStatus.Inactive &&  request.Status.Value != EPromotionStatus.Active))
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Khuyến mãi đang chạy chỉ cho phép chuyển từ Active sang Inactive (tắt khẩn cấp)"
                };
            }
        }

        // ─── Validate GlobalDiscountCap ──────────────────────────────
        if (request.GlobalDiscountCap.HasValue && request.GlobalDiscountCap.Value > 0)
        {
            if (promotion.GlobalDiscountCap.HasValue &&
                request.GlobalDiscountCap.Value < promotion.GlobalDiscountCap.Value)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Khuyến mãi đang chạy chỉ được tăng GlobalDiscountCap, không được giảm"
                };
            }
        }

        // ─── Ignore các field không được sửa khi đang chạy ──────────
        if (request.RuleConditions != null || request.RuleActions != null
                                           || request.PromotionType.HasValue || request.StartDate.HasValue ||
                                           request.Priority.HasValue)
        {
            _logger.Warning(
                "Promotion {Id} đang chạy — các field PromotionType/StartDate/Priority/Conditions/Actions " +
                "bị ignore vì không được phép sửa khi promotion đang active",
                promotion.Id
            );
        }

        // ─── Begin Transaction ───────────────────────────────────────
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

        var isStatusChage = request.Status.HasValue && (request.Status.Value != promotion.Status);
        // ─── Apply chỉ những field được phép ────────────────────────
        if (request.Name != null) promotion.Name = request.Name;
        if (request.ShortDescription != null) promotion.ShortDescription = request.ShortDescription;
        if (request.Description != null) promotion.Description = request.Description;
        if (request.EndDate.HasValue) promotion.EndDate = request.EndDate;
        if (request.Status.HasValue) promotion.Status = request.Status.Value;

        if (request.GlobalDiscountCap.HasValue)
        {
            promotion.GlobalDiscountCap = request.GlobalDiscountCap.Value == 0
                ? null
                : request.GlobalDiscountCap.Value;
        }

        promotion.LastModifiedDate = now;
        _unitOfWork.GetRepository<Domain.Entities.PromotionRules>().UpdateAsync(promotion);

        var successMessage = request.Status == EPromotionStatus.Inactive
            ? "Đã tắt khuyến mãi thành công"
            : "Cập nhật khuyến mãi thành công";

        return await CommitAndInvalidateCacheAsync(promotion, role, brandId, isStatusChage, successMessage);
    }

    // =========================================================
    // SHARED: Commit + Cache Invalidation
    // =========================================================

    private async Task<ApiResponse> CommitAndInvalidateCacheAsync(
        Domain.Entities.PromotionRules promotion,
        ERole? role,
        Guid brandId,
        bool isStatusChage,
        string successMessage)
    {
        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed khi update promotion {Id}: {Message}",
                promotion.Id, commitResult.Message
            );
            throw new Exception($"Không thể cập nhật khuyến mãi: {commitResult.Message}");
        }

        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
            ),
            operation: isStatusChage ? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
            ),
            entityCachePrefix: CacheConfig.EntityListCachePrefix($"{nameof(PromotionRules)}:{role}:{brandId}")
        );

        if (cacheResult.Success)
            _logger.Information(
                "Updated promotion '{Name}' (ID: {Id}). Cache invalidated.",
                promotion.Name, promotion.Id);
        else
            _logger.Warning(
                "Updated promotion '{Name}' nhưng cache invalidation có vấn đề: {Message}",
                promotion.Name, cacheResult.Message);

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = successMessage,
            Data = promotion.Id
        };
    }

    // =========================================================
    // SHARED: Replace All Conditions
    // =========================================================

    private async Task ReplaceConditionsAsync(Guid promotionId, List<ConditionDto> newConditions)
    {
        var existing = await _unitOfWork.GetRepository<RuleConditions>()
            .GetListAsync(predicate: x => x.PromotionRuleId == promotionId);

        if (existing.Any())
            _unitOfWork.GetRepository<RuleConditions>().DeleteRangeAsync(existing);

        var entities = newConditions.Select(c => new RuleConditions
        {
            Id = Guid.CreateVersion7(),
            PromotionRuleId = promotionId,
            ConditionType = c.ConditionType,
            Operator = c.Operator,
            Value = c.Value
        }).ToList();

        await _unitOfWork.GetRepository<RuleConditions>().InsertRangeAsync(entities);

        _logger.Information("Replace {Count} conditions cho promotion {Id}", entities.Count, promotionId);
    }

    // =========================================================
    // SHARED: Replace All Actions + Targets
    // =========================================================

    private async Task ReplaceActionsAsync(Guid promotionId, List<ActionDto> newActions)
    {
        // Lấy actions cũ để xóa targets trước
        var existingActions = await _unitOfWork.GetRepository<RuleActions>()
            .GetListAsync(
                predicate: x => x.PromotionRuleId == promotionId,
                include: q => q.Include(a => a.RuleActionTargets)
            );

        var existingTargets = existingActions
            .Where(a => a.RuleActionTargets != null)
            .SelectMany(a => a.RuleActionTargets!)
            .ToList();

        if (existingTargets.Any())
            _unitOfWork.GetRepository<RuleActionTargets>().DeleteRangeAsync(existingTargets);

        if (existingActions.Any())
            _unitOfWork.GetRepository<RuleActions>().DeleteRangeAsync(existingActions);

        foreach (var actionDto in newActions)
        {
            var action = new RuleActions
            {
                Id = Guid.CreateVersion7(),
                PromotionRuleId = promotionId,
                ActionType = actionDto.ActionType,
                // BuyXGetYFreeProducts, FreeGiftProduct, FreeShipping không cần Value
                Value = actionDto.ActionType is ERuleActionType.BuyXGetYFreeProducts
                    or ERuleActionType.FreeGiftProduct
                    or ERuleActionType.FreeShipping
                    ? null
                    : actionDto.Value,
                MaxDiscountAmountForPercentage = actionDto.MaxDiscountAmountForPercentage
            };

            await _unitOfWork.GetRepository<RuleActions>().InsertAsync(action);

            if (actionDto.Targets != null && actionDto.Targets.Any())
            {
                var targets = actionDto.Targets.Select(t => new RuleActionTargets
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

        _logger.Information("Replace {Count} actions cho promotion {Id}", newActions.Count, promotionId);
    }

    // =========================================================
    // VALIDATE ACTIONS THEO PROMOTION TYPE
    // =========================================================

    private static string? ValidateActionsByPromotionType(EPromotionType type, List<ActionDto> actions)
    {
        if (!actions.Any()) return "Khuyến mãi phải có ít nhất 1 action";

        return type switch
        {
            EPromotionType.OrderDiscount => ValidateOrderDiscountActions(actions),
            EPromotionType.LineItemDiscount => ValidateLineItemDiscountActions(actions),
            EPromotionType.BuyXGetY => ValidateBuyXGetYActions(actions),
            // EPromotionType.QuantityTier     => ValidateQuantityTierActions(actions),
            EPromotionType.FreeGift => ValidateFreeGiftActions(actions),
            EPromotionType.FreeShipping => ValidateFreeShippingActions(actions),
            _ => "PromotionType không hợp lệ"
        };
    }

    private static string? ValidateOrderDiscountActions(List<ActionDto> actions)
    {
        var allowed = new[] { ERuleActionType.CartPercentageDiscount, ERuleActionType.CartFixedDiscount };
        var invalid = actions.FirstOrDefault(a => !allowed.Contains(a.ActionType));
        if (invalid != null)
            return $"OrderDiscount không hỗ trợ action '{invalid.ActionType}'. " +
                   "Chỉ được dùng CartPercentageDiscount hoặc CartFixedDiscount";

        foreach (var a in actions)
        {
            if (a.ActionType == ERuleActionType.CartPercentageDiscount)
                if (!decimal.TryParse(a.Value, out var pct) || pct <= 0 || pct > 100)
                    return "Phần trăm giảm giá phải trong khoảng (0, 100]";

            if (a.ActionType == ERuleActionType.CartFixedDiscount)
                if (!decimal.TryParse(a.Value, out var amt) || amt <= 0)
                    return "Số tiền giảm cố định phải là số dương";
        }

        return null;
    }

    private static string? ValidateLineItemDiscountActions(List<ActionDto> actions)
    {
        var allowed = new[] { ERuleActionType.ItemPercentageDiscount, ERuleActionType.ItemFixedDiscount };
        var invalid = actions.FirstOrDefault(a => !allowed.Contains(a.ActionType));
        if (invalid != null)
            return $"LineItemDiscount không hỗ trợ action '{invalid.ActionType}'. " +
                   "Chỉ được dùng ItemPercentageDiscount hoặc ItemFixedDiscount";

        foreach (var a in actions)
        {
            var hasTarget = a.Targets != null && a.Targets.Any(t => t.Role == EActionTargetRole.DiscountTarget);
            if (!hasTarget)
                return "Mỗi action LineItemDiscount phải có ít nhất 1 DiscountTarget";
        }

        var allTargetIds = actions
            .Where(a => a.Targets != null)
            .SelectMany(a => a.Targets!.Where(t => t.Role == EActionTargetRole.DiscountTarget))
            .Select(t => t.TargetId)
            .ToList();

        if (allTargetIds.Count != allTargetIds.Distinct().Count())
            return "Không được có sản phẩm/danh mục trùng lặp giữa các actions";

        return null;
    }

    /// <summary>
    /// BuyXGetY KHÔNG dùng BuyProduct target.
    /// Thông tin "mua bao nhiêu sản phẩm nào" nằm trong condition MinQuantityOfProduct.
    /// </summary>
    private static string? ValidateBuyXGetYActions(List<ActionDto> actions)
    {
        if (actions.Count != 1)
            return "BuyXGetY chỉ được có đúng 1 action";

        var action = actions[0];

        if (action.ActionType != ERuleActionType.BuyXGetYFreeProducts)
            return "BuyXGetY phải dùng action 'BuyXGetYFreeProducts'";

        // Không được có BuyProduct target
        var hasBuyProductTarget = action.Targets != null &&
                                  action.Targets.Any(t => t.Role == EActionTargetRole.BuyProduct);
        if (hasBuyProductTarget)
            return "BuyXGetY không sử dụng target 'BuyProduct'. " +
                   "Sản phẩm cần mua và số lượng phải khai báo trong điều kiện 'MinQuantityOfProduct' (format: 'productId:minQty')";

        // Phải có ít nhất 1 GetProduct target
        var getTargets = action.Targets?.Where(t => t.Role == EActionTargetRole.GetProduct).ToList();

        if (getTargets == null || !getTargets.Any())
            return "BuyXGetY phải có ít nhất 1 target với role 'GetProduct' (sản phẩm được tặng)";

        if (getTargets.Any(t => t.TargetType != EActionTargetType.Product))
            return "GetProduct target của BuyXGetY phải là Product, không được dùng Category";

        if (getTargets.Any(t => t.Quantity < 1))
            return "Số lượng sản phẩm được tặng (GetProduct.Quantity) phải >= 1";

        return null;
    }

    private static string? ValidateQuantityTierActions(List<ActionDto> actions)
    {
        var allowed = new[]
        {
            ERuleActionType.CartPercentageDiscount,
            ERuleActionType.CartFixedDiscount,
            ERuleActionType.ItemPercentageDiscount,
            ERuleActionType.ItemFixedDiscount
        };

        var invalid = actions.FirstOrDefault(a => !allowed.Contains(a.ActionType));
        if (invalid != null)
            return $"QuantityTier không hỗ trợ action '{invalid.ActionType}'";

        foreach (var a in actions)
        {
            if (!decimal.TryParse(a.Value, out var val) || val <= 0)
                return "Giá trị discount của QuantityTier phải là số dương";

            if (a.ActionType is ERuleActionType.CartPercentageDiscount
                    or ERuleActionType.ItemPercentageDiscount && val > 100)
                return "Phần trăm giảm giá phải <= 100";

            if (a.ActionType is ERuleActionType.ItemPercentageDiscount
                or ERuleActionType.ItemFixedDiscount)
            {
                var hasTarget = a.Targets != null && a.Targets.Any(t => t.Role == EActionTargetRole.DiscountTarget);
                if (!hasTarget)
                    return $"Action '{a.ActionType}' trong QuantityTier phải có ít nhất 1 DiscountTarget";
            }
        }

        return null;
    }

    private static string? ValidateFreeGiftActions(List<ActionDto> actions)
    {
        if (actions.Count != 1)
            return "FreeGift chỉ được có đúng 1 action";

        if (actions[0].ActionType != ERuleActionType.FreeGiftProduct)
            return "FreeGift phải dùng action 'FreeGiftProduct'";

        var giftTargets = actions[0].Targets?.Where(t => t.Role == EActionTargetRole.GiftProduct).ToList();

        if (giftTargets == null || giftTargets.Count != 1)
            return "FreeGift phải có đúng 1 GiftProduct target";

        if (giftTargets[0].TargetType != EActionTargetType.Product)
            return "GiftProduct target phải là Product";

        if (giftTargets[0].Quantity < 1)
            return "Số lượng quà tặng phải >= 1";

        return null;
    }

    private static string? ValidateFreeShippingActions(List<ActionDto> actions)
    {
        if (actions.Count != 1)
            return "FreeShipping chỉ được có đúng 1 action";

        if (actions[0].ActionType != ERuleActionType.FreeShipping)
            return "FreeShipping promotion chỉ được dùng action 'FreeShipping'";

        if (actions[0].Targets != null && actions[0].Targets.Any())
            return "FreeShipping action không cần target";

        return null;
    }

    // =========================================================
    // VALIDATE CONDITIONS BASIC (không cần DB)
    // =========================================================

    private static string? ValidateConditionsBasic(List<ConditionDto> conditions)
    {
        var duplicate = conditions.GroupBy(c => c.ConditionType).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            return $"Không được có 2 điều kiện cùng loại '{duplicate.Key}' trong 1 khuyến mãi";

        return null;
    }

    // =========================================================
    // VALIDATE DB EXISTENCE
    // =========================================================

    private async Task<string?> ValidateConditionTargetsExistAsync(
        List<ConditionDto> conditions,
        Guid brandId)
    {
        foreach (var condition in conditions)
        {
            switch (condition.ConditionType)
            {
                case ERuleConditionType.CartContainsProduct:
                {
                    var ids = ParseGuidList(condition.Value);
                    if (ids == null) return "CartContainsProduct value phải là danh sách GUID hợp lệ";

                    var existing = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                        .GetListAsync(predicate: x => ids.Contains(x.Id));
                    if (existing.Count != ids.Count)
                        return "Một hoặc nhiều sản phẩm trong điều kiện CartContainsProduct không tồn tại";
                    break;
                }

                case ERuleConditionType.CartContainsCategory:
                {
                    var ids = ParseGuidList(condition.Value);
                    if (ids == null) return "CartContainsCategory value phải là danh sách GUID hợp lệ";

                    var existing = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                        .GetListAsync(predicate: x => ids.Contains(x.Id) && x.BrandId == brandId);
                    if (existing.Count != ids.Count)
                        return "Một hoặc nhiều danh mục trong CartContainsCategory không tồn tại " +
                               "hoặc không thuộc thương hiệu này";
                    break;
                }

                case ERuleConditionType.MinQuantityOfProduct:
                {
                    var parsed = ParseIdColonQty(condition.Value);
                    if (parsed == null)
                        return "MinQuantityOfProduct value phải theo format 'productId:minQty' (VD: 'uuid:3')";

                    var exists = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                        .SingleOrDefaultAsync(predicate: x => x.Id == parsed.Value.Id);
                    if (exists == null)
                        return $"Sản phẩm '{parsed.Value.Id}' trong MinQuantityOfProduct không tồn tại";
                    break;
                }

                case ERuleConditionType.MinQuantityInCategory:
                {
                    var parsed = ParseIdColonQty(condition.Value);
                    if (parsed == null)
                        return "MinQuantityInCategory value phải theo format 'categoryId:minQty' (VD: 'uuid:5')";

                    var exists = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                        .SingleOrDefaultAsync(predicate: x => x.Id == parsed.Value.Id && x.BrandId == brandId);
                    if (exists == null)
                        return $"Danh mục '{parsed.Value.Id}' trong MinQuantityInCategory không tồn tại";
                    break;
                }
            }
        }

        return null;
    }

    private async Task<string?> ValidateActionTargetsExistAsync(List<TargetDto> targets, Guid brandId)
    {
        var productIds = targets
            .Where(t => t.TargetType == EActionTargetType.Product)
            .Select(t => t.TargetId).Distinct().ToList();

        var categoryIds = targets
            .Where(t => t.TargetType == EActionTargetType.Category)
            .Select(t => t.TargetId).Distinct().ToList();

        if (productIds.Any())
        {
            var existing = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                .GetListAsync(predicate: x => productIds.Contains(x.Id));
            if (existing.Count != productIds.Count)
                return "Một hoặc nhiều sản phẩm trong danh sách target không tồn tại";
        }

        if (categoryIds.Any())
        {
            var existing = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                .GetListAsync(predicate: x => categoryIds.Contains(x.Id) && x.BrandId == brandId);
            if (existing.Count != categoryIds.Count)
                return "Một hoặc nhiều danh mục trong danh sách target không tồn tại " +
                       "hoặc không thuộc thương hiệu này";
        }

        return null;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static PromotionLifecycleState DetermineLifecycleState(
        Domain.Entities.PromotionRules promotion,
        DateTime now)
    {
        if (promotion.Status == EPromotionStatus.Inactive) return PromotionLifecycleState.Inactive;
        if (promotion.Status == EPromotionStatus.Draft) return PromotionLifecycleState.NotStarted;
        if (promotion.EndDate.HasValue && now > promotion.EndDate.Value) return PromotionLifecycleState.Expired;
        if (promotion.StartDate.HasValue && now < promotion.StartDate.Value) return PromotionLifecycleState.NotStarted;
        return PromotionLifecycleState.Running;
    }

    private static List<Guid>? ParseGuidList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.Parse(s.Trim())).ToList();
        }
        catch
        {
            return null;
        }
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
        catch
        {
            return null;
        }
    }
}

// ─── Internal DTOs ─────────────────────────────────────────────────────────

internal record ConditionDto
{
    public ERuleConditionType ConditionType { get; init; }
    public ERuleConditionOperator Operator { get; init; }
    public string? Value { get; init; }
}

internal record ActionDto
{
    public ERuleActionType ActionType { get; init; }
    public string? Value { get; init; }
    public decimal? MaxDiscountAmountForPercentage { get; init; }
    public List<TargetDto>? Targets { get; init; }
}

internal record TargetDto
{
    public EActionTargetType TargetType { get; init; }
    public Guid TargetId { get; init; }
    public int Quantity { get; init; }
    public EActionTargetRole Role { get; init; }
}

internal enum PromotionLifecycleState
{
    NotStarted,
    Running,
    Expired,
    Inactive
}