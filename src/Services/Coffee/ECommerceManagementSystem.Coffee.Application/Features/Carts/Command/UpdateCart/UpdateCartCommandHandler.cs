using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.UpdateCart;

public class UpdateCartCommandHandler : IRequestHandler<UpdateCartCommand, ApiResponse>
{
    private readonly ILogger _logger;
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    private const string MetadataField = "metadata";

    public UpdateCartCommandHandler(
        ILogger logger,
        IRedisService redisService,
        IClaimService claimService,
        IMediaService mediaService,
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork)
    {
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<ApiResponse> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        // ─── STEP 1: Auth ────────────────────────────────────────────
        var role = _claimService.GetCurrentRoleEnum();
        if (role != ERole.EndCustomer)
        {
            _logger.Warning("Unauthorized cart update attempt by role: {Role}", role);
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Chỉ khách hàng mới có thể cập nhật giỏ hàng!"
            };
        }

        var customerId = _claimService.GetCurrentReferenceId();
        if (customerId == Guid.Empty)
        {
            _logger.Error("Invalid customer ID");
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Thông tin khách hàng không hợp lệ!"
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

        // ─── STEP 2: Validate items ──────────────────────────────────
        if (request.Items == null || !request.Items.Any())
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Giỏ hàng phải có ít nhất một sản phẩm!"
            };
        }

        if (request.Items.Any(i => i.Quantity < 0))
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Số lượng sản phẩm không hợp lệ!"
            };
        }

        // ─── STEP 3: Get or create cart ──────────────────────────────
        var hashKey = BuildHashKey(customerId);
        var (cart, cartError) = await GetOrCreateCart(hashKey, customerId, request.CartId);
        if (cartError != null) return cartError;
        if (cart == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể tạo giỏ hàng, vui lòng thử lại!"
            };
        }

        // ─── STEP 4: Validate ownership ──────────────────────────────
        if (request.CartId.HasValue && cart.CustomerId != customerId)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn không có quyền cập nhật giỏ hàng này!"
            };
        }

        // ─── STEP 5: Load products từ DB ─────────────────────────────
        // Chỉ load non-gift items (FE không bao giờ gửi gift items lên)
        var activeProductIds = request.Items
            .Where(i => i.Quantity > 0)
            .Select(i => i.ProductId)
            .ToList();

        var products = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetListAsync(
                predicate: x => activeProductIds.Contains(x.Id),
                include: q => q.Include(p => p.ProductImages)
            );

        if (products == null || products.Count != activeProductIds.Count)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Một hoặc nhiều sản phẩm không tồn tại!"
            };
        }

        foreach (var requestItem in request.Items.Where(i => i.Quantity > 0))
        {
            var product = products.First(p => p.Id == requestItem.ProductId);
            if (requestItem.Quantity > product.StockQuantity)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} trong kho!"
                };
            }
        }

        // ─── STEP 6: Build non-gift items từ DB data ─────────────────
        // Price và Name luôn lấy từ DB để đảm bảo chính xác
        // Chỉ lấy quantity và imageUrl từ request
        cart.Items = request.Items
            .Where(i => i.Quantity > 0)
            .Select(requestItem =>
            {
                var product = products.First(p => p.Id == requestItem.ProductId);
                var mainImage = product.ProductImages
                                    .FirstOrDefault(img => img.IsMainImage)
                                ?? product.ProductImages.FirstOrDefault();

                return new GetCustomerCartItemsResponse
                {
                    GetCustomerCartId = cart.Id,
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    ProductImageUrlSnapshot = requestItem.ProductImageUrlSnapshot
                                              ?? mainImage?.ImageUrl,
                    Quantity = requestItem.Quantity,
                    UnitPriceSnapshot = product.Price ?? 0,
                    TotalAmountSnapshot = (product.Price ?? 0) * requestItem.Quantity,
                    IsGiftItem = false,
                    PromotionId = null,
                };
            })
            .ToList();

        // Gift items sẽ được thêm lại ở STEP 7 sau khi validate promotions.
        // FE không bao giờ gửi gift items — luôn rebuild từ promotions hiện tại.

        // ─── STEP 7: Sync AppliedPromotions từ request ───────────────
        // Nếu FE gửi appliedPromotions → replace toàn bộ danh sách cũ.
        // Trường hợp này xảy ra khi FE xóa 1 mã (gửi danh sách đã filter bỏ mã đó).
        // Nếu FE không gửi (null) → giữ nguyên danh sách cũ trong cart.
        if (request.AppliedPromotions != null)
        {
            cart.AppliedPromotions = request.AppliedPromotions
                .Select(promo => new GetCustomerCartAppliedPromotionsResponse
                {
                    GetCustomerCartId = cart.Id,
                    PromotionId = promo.PromotionRuleId,
                    PromotionRuleCode = promo.PromotionRuleCode,
                    PromotionRuleNameSnapshot = promo.PromotionRuleNameSnapshot,
                    DiscountAmountApplied = promo.DiscountAmountApplied,
                    // StackingSlot sẽ được re-set khi re-validate bên dưới
                    CreatedDate = (decimal)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                })
                .ToList();
        }

        // ─── STEP 8: Re-validate promotions hiện có ──────────────────
        // Sau khi items thay đổi, cần kiểm tra lại từng promotion:
        //   - Hết hạn / bị tắt → tự xóa
        //   - Không còn đủ điều kiện → tự xóa
        //   - Còn hợp lệ → recalculate discount + rebuild gift items
        if (cart.AppliedPromotions.Any())
        {
            var now = DateTime.UtcNow;
            var validatedPromotions = new List<GetCustomerCartAppliedPromotionsResponse>();

            foreach (var appliedPromo in cart.AppliedPromotions)
            {
                var promotion = await _unitOfWork
                    .GetRepository<Domain.Entities.PromotionRules>()
                    .SingleOrDefaultAsync(
                        predicate: x =>
                            x.Id == appliedPromo.PromotionId
                            && x.BrandId == existedBrand.Id
                            && x.Status == EPromotionStatus.Active
                            && x.StartDate <= now
                            && x.EndDate >= now,
                        include: q => q
                            .Include(p => p.RuleConditions)
                            .Include(p => p.RuleActions)
                            .ThenInclude(a => a.RuleActionTargets)
                    );

                if (promotion == null)
                {
                    // Promotion hết hạn hoặc bị tắt → tự xóa
                    _logger.Warning(
                        "Promotion {Id} hết hạn hoặc bị tắt, tự động xóa khỏi cart {CartId}",
                        appliedPromo.PromotionId, cart.Id);
                    continue;
                }

                // Re-evaluate conditions với items mới
                var (stillMet, _) = await EvaluateConditionsAsync(promotion.RuleConditions, cart, customerId);
                if (!stillMet)
                {
                    _logger.Information(
                        "Promotion {Code} không còn đủ điều kiện sau khi đổi items, tự động xóa",
                        promotion.Code);
                    continue;
                }

                // Recalculate discount với items mới
                var discountResult = CalculateDiscount(promotion, cart);
                appliedPromo.DiscountAmountApplied = discountResult.IsApplicable
                    ? discountResult.DiscountAmount
                    : 0;

                // Cập nhật lại StackingSlot (phòng trường hợp FE không gửi)
                appliedPromo.StackingSlot = promotion.PromotionType == EPromotionType.FreeShipping
                    ? EStackingSlot.FreeShipping
                    : EStackingSlot.BestDiscount;

                validatedPromotions.Add(appliedPromo);

                // Rebuild gift items cho promotion này (BuyXGetY / FreeGift)
                await AddGiftItemsToCart(cart, promotion);
            }

            cart.AppliedPromotions = validatedPromotions;
        }

        // ─── STEP 9: Apply promotion code mới nếu có ─────────────────
        if (!string.IsNullOrWhiteSpace(request.PromotionCodeToApply))
        {
            var applyResult = await ApplyPromotionCodeAsync(
                cart, request.PromotionCodeToApply.Trim().ToUpper(), customerId);

            if (applyResult != null) return applyResult; // trả lỗi nếu không apply được
        }

        // ─── STEP 10: CustomerNote ────────────────────────────────────
        cart.CustomerNote = request.CustomerNote;

        // ─── STEP 11: RecalculateTotals ───────────────────────────────
        RecalculateTotals(cart);
        cart.LastModifiedDate = DateTime.UtcNow;

        // ─── STEP 12: Persist vào Redis ───────────────────────────────
        var cartField = BuildCartField(cart.Id);
        await _redisService.SetHashAsync(hashKey, cartField, JsonSerializer.Serialize(cart));

        _logger.Information(
            "Cart {CartId} updated for customer {CustomerId}: {NonGiftCount} items, {GiftCount} gift items, {PromoCount} promotions",
            cart.Id, customerId,
            cart.Items.Count(i => !i.IsGiftItem),
            cart.Items.Count(i => i.IsGiftItem),
            cart.AppliedPromotions.Count);

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Cập nhật giỏ hàng thành công!",
            Data = cart
        };
    }

    // =========================================================
    // APPLY PROMOTION CODE (STEP 9)
    // =========================================================

    /// <summary>
    /// Tìm, validate và apply promotion code mới vào cart.
    /// Trả về null nếu thành công, trả về ApiResponse lỗi nếu thất bại.
    /// </summary>
    private async Task<ApiResponse?> ApplyPromotionCodeAsync(
        GetCustomerCartResponse cart,
        string code,
        Guid customerId)
    {
        var now = DateTime.UtcNow;

        var promotion = await _unitOfWork
            .GetRepository<Domain.Entities.PromotionRules>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    x.Code.Trim().ToUpper().Equals(code.ToUpper())
                    && x.Status == EPromotionStatus.Active
                    && x.StartDate <= now
                    && x.EndDate >= now,
                include: q => q
                    .Include(p => p.RuleConditions)
                    .Include(p => p.RuleActions)
                    .ThenInclude(a => a.RuleActionTargets)
            );

        if (promotion == null)
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Mã khuyến mãi không tồn tại hoặc đã hết hạn!"
            };

        // Đã apply rồi
        if (cart.AppliedPromotions.Any(p => p.PromotionId == promotion.Id))
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Mã khuyến mãi này đã được áp dụng!"
            };

        // Kiểm tra stacking slot
        var incomingSlot = promotion.PromotionType == EPromotionType.FreeShipping
            ? EStackingSlot.FreeShipping
            : EStackingSlot.BestDiscount;

        if (cart.AppliedPromotions.Any(p => p.StackingSlot == incomingSlot))
        {
            var slotLabel = incomingSlot == EStackingSlot.FreeShipping
                ? "miễn phí vận chuyển"
                : "giảm giá";
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = $"Đã có 1 mã {slotLabel} được áp dụng. Vui lòng xóa mã hiện tại trước!"
            };
        }

        // Evaluate conditions
        var (conditionsMet, conditionError) = await EvaluateConditionsAsync(promotion.RuleConditions, cart, customerId);
        if (!conditionsMet)
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = conditionError ?? "Giỏ hàng chưa đủ điều kiện áp dụng mã này!"
            };

        // Calculate discount
        var discountResult = CalculateDiscount(promotion, cart);
        if (!discountResult.IsApplicable)
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = discountResult.ErrorMessage ?? "Không thể áp dụng mã khuyến mãi này!"
            };

        // Add promotion vào cart
        cart.AppliedPromotions.Add(new GetCustomerCartAppliedPromotionsResponse
        {
            GetCustomerCartId = cart.Id,
            PromotionId = promotion.Id,
            PromotionRuleCode = promotion.Code,
            PromotionRuleNameSnapshot = promotion.Name,
            DiscountAmountApplied = discountResult.DiscountAmount,
            StackingSlot = incomingSlot,
            CreatedDate = (decimal)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        // Add gift items nếu là BuyXGetY hoặc FreeGift
        await AddGiftItemsToCart(cart, promotion);

        _logger.Information(
            "Promotion {Code} applied to cart {CartId}. Discount: {Amount}",
            promotion.Code, cart.Id, discountResult.DiscountAmount);

        return null; // null = thành công
    }

    // =========================================================
    // ADD GIFT ITEMS
    // =========================================================

    /// <summary>
    /// Thêm gift items vào cart cho promotion BuyXGetY hoặc FreeGift.
    ///
    /// BuyXGetY:
    ///   timesApplicable được tính từ condition MinQuantityOfProduct
    ///   (không dùng BuyProduct target — đã bỏ theo thiết kế)
    ///   timesApplicable = floor(cartQty_buyProduct / requiredQty)
    ///
    /// FreeGift:
    ///   Tặng cố định số lượng theo GiftProduct target.
    /// </summary>
    private async Task AddGiftItemsToCart(
        GetCustomerCartResponse cart,
        Domain.Entities.PromotionRules promotion)
    {
        foreach (var action in promotion.RuleActions)
        {
            if (action.ActionType == ERuleActionType.BuyXGetYFreeProducts)
            {
                await AddBuyXGetYGiftItems(cart, promotion, action);
            }
            else if (action.ActionType == ERuleActionType.FreeGiftProduct)
            {
                await AddFreeGiftItems(cart, promotion, action);
            }
        }
    }

    private async Task AddBuyXGetYGiftItems(
        GetCustomerCartResponse cart,
        Domain.Entities.PromotionRules promotion,
        RuleActions action)
    {
        // ─── Tính timesApplicable từ condition MinQuantityOfProduct ──
        // Đây là điểm khác biệt quan trọng so với thiết kế cũ:
        // Không đọc từ BuyProduct target mà đọc từ condition.
        var minQtyCondition = promotion.RuleConditions
            .FirstOrDefault(c => c.ConditionType == ERuleConditionType.MinQuantityOfProduct);

        if (minQtyCondition == null)
        {
            _logger.Warning(
                "BuyXGetY promotion {Code} thiếu condition MinQuantityOfProduct, bỏ qua gift items",
                promotion.Code);
            return;
        }

        var parsed = ParseIdColonQty(minQtyCondition.Value);
        if (parsed == null)
        {
            _logger.Warning(
                "BuyXGetY promotion {Code}: MinQuantityOfProduct value '{Value}' không hợp lệ",
                promotion.Code, minQtyCondition.Value);
            return;
        }

        var (buyProductId, requiredQty) = parsed.Value;

        // Chỉ tính non-gift items khi đếm số lượng mua
        var cartQty = cart.Items
            .Where(i => !i.IsGiftItem && i.ProductId == buyProductId)
            .Sum(i => i.Quantity);

        var timesApplicable = (int)Math.Floor((double)cartQty / requiredQty);

        if (timesApplicable <= 0)
        {
            _logger.Information(
                "BuyXGetY {Code}: cartQty={CartQty}, requiredQty={RequiredQty} → timesApplicable=0, bỏ qua",
                promotion.Code, cartQty, requiredQty);
            return;
        }

        // ─── Load GetProduct targets ─────────────────────────────────
        var getTargets = action.RuleActionTargets
            .Where(t => t.Role == EActionTargetRole.GetProduct)
            .ToList();

        if (!getTargets.Any())
        {
            _logger.Warning(
                "BuyXGetY promotion {Code} không có GetProduct target",
                promotion.Code);
            return;
        }

        var getProductIds = getTargets.Select(t => t.TargetId).ToList();
        var getProducts = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetListAsync(
                predicate: x => getProductIds.Contains(x.Id),
                include: q => q.Include(p => p.ProductImages)
            );

        // Xóa gift items cũ của promotion này trước khi thêm mới
        // (tránh duplicate khi rebuild)
        cart.Items.RemoveAll(i => i.IsGiftItem && i.PromotionId == promotion.Id);

        foreach (var getTarget in getTargets)
        {
            var giftProduct = getProducts?.FirstOrDefault(p => p.Id == getTarget.TargetId);
            if (giftProduct == null)
            {
                _logger.Warning(
                    "GetProduct {ProductId} trong promotion {Code} không tìm thấy trong DB",
                    getTarget.TargetId, promotion.Code);
                continue;
            }

            var mainImage = giftProduct.ProductImages
                                .FirstOrDefault(img => img.IsMainImage)
                            ?? giftProduct.ProductImages.FirstOrDefault();

            // Tổng số lượng tặng = số lần áp dụng × số lượng mỗi lần
            var giftQty = getTarget.Quantity * timesApplicable;

            cart.Items.Add(new GetCustomerCartItemsResponse
            {
                GetCustomerCartId = cart.Id,
                ProductId = giftProduct.Id,
                ProductNameSnapshot = giftProduct.Name,
                ProductImageUrlSnapshot = mainImage?.ImageUrl,
                Quantity = giftQty,
                UnitPriceSnapshot = 0,
                TotalAmountSnapshot = 0,
                IsGiftItem = true,
                PromotionId = promotion.Id,
            });

            _logger.Information(
                "BuyXGetY {Code}: Added gift {ProductName} x{Qty} (timesApplicable={Times} × qty={TargetQty})",
                promotion.Code, giftProduct.Name, giftQty, timesApplicable, getTarget.Quantity);
        }
    }

    private async Task AddFreeGiftItems(
        GetCustomerCartResponse cart,
        Domain.Entities.PromotionRules promotion,
        RuleActions action)
    {
        var giftTargets = action.RuleActionTargets
            .Where(t => t.Role == EActionTargetRole.GiftProduct)
            .ToList();

        if (!giftTargets.Any())
        {
            _logger.Warning(
                "FreeGift promotion {Code} không có GiftProduct target",
                promotion.Code);
            return;
        }

        var giftProductIds = giftTargets.Select(t => t.TargetId).ToList();
        var giftProducts = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetListAsync(
                predicate: x => giftProductIds.Contains(x.Id),
                include: q => q.Include(p => p.ProductImages)
            );

        // Xóa gift items cũ của promotion này trước khi thêm mới
        cart.Items.RemoveAll(i => i.IsGiftItem && i.PromotionId == promotion.Id);

        foreach (var giftTarget in giftTargets)
        {
            var giftProduct = giftProducts?.FirstOrDefault(p => p.Id == giftTarget.TargetId);
            if (giftProduct == null)
            {
                _logger.Warning(
                    "GiftProduct {ProductId} trong promotion {Code} không tìm thấy trong DB",
                    giftTarget.TargetId, promotion.Code);
                continue;
            }

            var mainImage = giftProduct.ProductImages
                                .FirstOrDefault(img => img.IsMainImage)
                            ?? giftProduct.ProductImages.FirstOrDefault();

            cart.Items.Add(new GetCustomerCartItemsResponse
            {
                GetCustomerCartId = cart.Id,
                ProductId = giftProduct.Id,
                ProductNameSnapshot = giftProduct.Name,
                ProductImageUrlSnapshot = mainImage?.ImageUrl,
                Quantity = giftTarget.Quantity,
                UnitPriceSnapshot = 0,
                TotalAmountSnapshot = 0,
                IsGiftItem = true,
                PromotionId = promotion.Id,
            });

            _logger.Information(
                "FreeGift {Code}: Added gift {ProductName} x{Qty}",
                promotion.Code, giftProduct.Name, giftTarget.Quantity);
        }
    }

    // =========================================================
    // EVALUATE CONDITIONS
    // =========================================================

    /// <summary>
    /// Tất cả conditions được AND lại — phải thỏa tất cả.
    /// CartContainsCategory / MinQuantityInCategory cần join DB → skip ở cart,
    /// validate đầy đủ khi CreateOrder.
    /// </summary>
    private async Task<(bool Met, string? Error)> EvaluateConditionsAsync(
        ICollection<RuleConditions> conditions,
        GetCustomerCartResponse cart,
        Guid customerId)
    {
        // Chỉ tính non-gift items khi check conditions
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
                        return (false, "Điều kiện giá trị đơn hàng không hợp lệ");

                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.GreaterThanOrEqual => cartSubtotal >= threshold,
                        ERuleConditionOperator.GreaterThan => cartSubtotal > threshold,
                        ERuleConditionOperator.Equals => cartSubtotal == threshold,
                        _ => false
                    };

                    if (!passed)
                        return (false,
                            $"Đơn hàng cần tối thiểu {threshold:N0}đ để áp dụng mã này " +
                            $"(hiện tại: {cartSubtotal:N0}đ)");
                    break;
                }

                case ERuleConditionType.CartContainsProduct:
                {
                    var requiredIds = ParseGuidList(condition.Value);
                    if (requiredIds == null)
                        return (false, "Điều kiện sản phẩm không hợp lệ");

                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.ContainsAny => requiredIds.Any(id => cartProductIds.Contains(id)),
                        ERuleConditionOperator.ContainsAll => requiredIds.All(id => cartProductIds.Contains(id)),
                        _ => false
                    };

                    if (!passed)
                        return (false, "Giỏ hàng chưa có sản phẩm cần thiết để áp dụng mã này");
                    break;
                }

                case ERuleConditionType.TotalCartQuantity:
                {
                    if (!int.TryParse(condition.Value, out var minQty))
                        return (false, "Điều kiện số lượng không hợp lệ");

                    var passed = condition.Operator switch
                    {
                        ERuleConditionOperator.GreaterThanOrEqual => cartTotalQty >= minQty,
                        ERuleConditionOperator.GreaterThan => cartTotalQty > minQty,
                        ERuleConditionOperator.Equals => cartTotalQty == minQty,
                        _ => false
                    };

                    if (!passed)
                        return (false,
                            $"Cần mua tối thiểu {minQty} sản phẩm để áp dụng mã này " +
                            $"(hiện tại: {cartTotalQty})");
                    break;
                }

                case ERuleConditionType.MinQuantityOfProduct:
                {
                    var parsed = ParseIdColonQty(condition.Value);
                    if (parsed == null)
                        return (false, "Điều kiện số lượng sản phẩm không hợp lệ");

                    var (productId, minQty) = parsed.Value;

                    // Chỉ đếm non-gift items
                    var itemQty = nonGiftItems
                        .Where(i => i.ProductId == productId)
                        .Sum(i => i.Quantity);

                    if (itemQty < minQty)
                        return (false,
                            $"Cần mua tối thiểu {minQty} sản phẩm này để áp dụng mã " +
                            $"(hiện tại: {itemQty})");
                    break;
                }
                case ERuleConditionType.FirstOrder:
                {
                    // Đếm số đơn hàng không bị huỷ của customer.
                    // WaitingPayment cũng được tính — để tránh case customer mở nhiều tab
                    // tạo nhiều đơn cùng lúc rồi apply FirstOrder cho tất cả.
                    var existingOrderCount = await _unitOfWork
                        .GetRepository<Domain.Entities.Orders>()
                        .AnyAsync(predicate: x =>
                            x.CustomerId == customerId
                            && x.OrderStatus != EOrderStatus.Cancelled);

                    if (existingOrderCount)
                        return (false, "Ưu đãi này chỉ áp dụng cho đơn hàng đầu tiên của bạn!");
                    break;
                }
                case ERuleConditionType.CartContainsCategory:
                case ERuleConditionType.MinQuantityInCategory:
                    // Cần product→category join — skip ở cart, validate khi CreateOrder
                    break;
            }
        }

        return (true, null);
    }

    // =========================================================
    // CALCULATE DISCOUNT
    // =========================================================

    /// <summary>
    /// Tính discount amount cho 1 promotion.
    /// BuyXGetY và FreeGift: discount = 0 (gift items track qua IsGiftItem).
    /// FreeShipping: discount = TotalOrderShippingFee.
    /// </summary>
    internal static DiscountResult CalculateDiscount(
        Domain.Entities.PromotionRules promotion,
        GetCustomerCartResponse cart)
    {
        // Tính trên non-gift items
        var nonGiftItems = cart.Items.Where(i => !i.IsGiftItem).ToList();
        var cartSubtotal = nonGiftItems.Sum(i => i.TotalAmountSnapshot);

        // effectiveSubtotal = subtotal sau khi trừ discount BestDiscount đã apply
        // (OrderDiscount / ItemDiscount tính trên phần còn lại sau discount đầu)
        var existingBestDiscount = cart.AppliedPromotions
            .Where(p => p.StackingSlot == EStackingSlot.BestDiscount)
            .Sum(p => p.DiscountAmountApplied);
        var effectiveSubtotal = Math.Max(0, cartSubtotal - existingBestDiscount);

        decimal discountAmount = 0;

        foreach (var action in promotion.RuleActions)
        {
            switch (action.ActionType)
            {
                case ERuleActionType.CartPercentageDiscount:
                {
                    if (!decimal.TryParse(action.Value, out var pct))
                        return DiscountResult.Invalid("Cấu hình khuyến mãi không hợp lệ (CartPercentageDiscount)");

                    var raw = effectiveSubtotal * pct / 100;
                    discountAmount += action.MaxDiscountAmountForPercentage.HasValue
                        ? Math.Min(raw, action.MaxDiscountAmountForPercentage.Value)
                        : raw;
                    break;
                }

                case ERuleActionType.CartFixedDiscount:
                {
                    if (!decimal.TryParse(action.Value, out var amount))
                        return DiscountResult.Invalid("Cấu hình khuyến mãi không hợp lệ (CartFixedDiscount)");

                    discountAmount += Math.Min(amount, effectiveSubtotal);
                    break;
                }

                case ERuleActionType.ItemPercentageDiscount:
                {
                    if (!decimal.TryParse(action.Value, out var pct))
                        return DiscountResult.Invalid("Cấu hình khuyến mãi không hợp lệ (ItemPercentageDiscount)");

                    var targetIds = action.RuleActionTargets
                        .Where(t => t.Role == EActionTargetRole.DiscountTarget)
                        .Select(t => t.TargetId)
                        .ToHashSet();

                    // Chỉ tính non-gift items
                    foreach (var item in nonGiftItems.Where(i => targetIds.Contains(i.ProductId)))
                    {
                        var raw = item.TotalAmountSnapshot * pct / 100;
                        discountAmount += action.MaxDiscountAmountForPercentage.HasValue
                            ? Math.Min(raw, action.MaxDiscountAmountForPercentage.Value)
                            : raw;
                    }

                    break;
                }

                case ERuleActionType.ItemFixedDiscount:
                {
                    if (!decimal.TryParse(action.Value, out var amount))
                        return DiscountResult.Invalid("Cấu hình khuyến mãi không hợp lệ (ItemFixedDiscount)");

                    var targetIds = action.RuleActionTargets
                        .Where(t => t.Role == EActionTargetRole.DiscountTarget)
                        .Select(t => t.TargetId)
                        .ToHashSet();

                    foreach (var item in nonGiftItems.Where(i => targetIds.Contains(i.ProductId)))
                        discountAmount += Math.Min(amount * item.Quantity, item.TotalAmountSnapshot);
                    break;
                }

                case ERuleActionType.FreeShipping:
                    // Discount = shipping fee hiện tại (không cần Value)
                    discountAmount = cart.TotalOrderShippingFee;
                    break;

                case ERuleActionType.BuyXGetYFreeProducts:
                case ERuleActionType.FreeGiftProduct:
                    // Gift items: discount = 0, gift được track qua IsGiftItem
                    discountAmount = 0;
                    break;
            }
        }

        // Apply GlobalDiscountCap
        if (promotion.GlobalDiscountCap.HasValue && promotion.GlobalDiscountCap.Value > 0)
            discountAmount = Math.Min(discountAmount, promotion.GlobalDiscountCap.Value);

        // Đảm bảo discount không vượt subtotal và không âm
        discountAmount = Math.Min(discountAmount, cartSubtotal);
        discountAmount = Math.Max(0, discountAmount);

        return new DiscountResult
        {
            IsApplicable = true,
            DiscountAmount = discountAmount,
            PromotionType = promotion.PromotionType
        };
    }

    // =========================================================
    // RECALCULATE TOTALS
    // =========================================================

    internal static void RecalculateTotals(GetCustomerCartResponse cart)
    {
        // Chỉ tính non-gift items vào subtotal
        cart.TotalAmountWithoutDiscount = cart.Items
            .Where(i => !i.IsGiftItem)
            .Sum(i => i.TotalAmountSnapshot);

        cart.TotalOrderDiscount = cart.AppliedPromotions
            .Sum(p => p.DiscountAmountApplied);

        cart.TotalAmount = Math.Max(0,
            cart.TotalAmountWithoutDiscount
            - cart.TotalOrderDiscount
            + cart.TotalOrderShippingFee);
    }

    // =========================================================
    // REDIS HELPERS
    // =========================================================

    private async Task<(GetCustomerCartResponse? Cart, ApiResponse? Error)> GetOrCreateCart(
        string hashKey,
        Guid customerId,
        Guid? requestedCartId)
    {
        var metadata = await GetMetadata(hashKey);

        // Case 1: FE gửi cartId cụ thể
        if (requestedCartId.HasValue)
        {
            var cartJson = await _redisService.GetHashAsync(
                hashKey, BuildCartField(requestedCartId.Value));

            if (!string.IsNullOrEmpty(cartJson))
            {
                var cart = JsonSerializer.Deserialize<GetCustomerCartResponse>(cartJson);
                if (cart != null) return (cart, null);
            }

            _logger.Warning(
                "Cart {CartId} not found for customer {CustomerId}",
                requestedCartId.Value, customerId);

            return (null, new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy giỏ hàng. Vui lòng tải lại trang!"
            });
        }

        // Case 2: FE không gửi cartId → lấy active cart
        if (metadata.CartCount > 0 && metadata.ActiveCartId.HasValue)
        {
            var cartJson = await _redisService.GetHashAsync(
                hashKey, BuildCartField(metadata.ActiveCartId.Value));

            if (!string.IsNullOrEmpty(cartJson))
            {
                var existingCart = JsonSerializer.Deserialize<GetCustomerCartResponse>(cartJson);
                if (existingCart != null)
                {
                    _logger.Information(
                        "Using active cart {CartId} for customer {CustomerId}",
                        existingCart.Id, customerId);
                    return (existingCart, null);
                }
            }

            _logger.Warning(
                "Active cart {CartId} in metadata not found in Redis (expired?), creating new",
                metadata.ActiveCartId.Value);
        }

        // Case 3: Không có cart nào → tạo mới
        var newCartId = Guid.CreateVersion7();
        var newCart = new GetCustomerCartResponse
        {
            Id = newCartId,
            CustomerId = customerId,
            CartName = "Giỏ hàng chính",
            IsActive = true,
            TotalAmountWithoutDiscount = 0,
            TotalOrderDiscount = 0,
            TotalOrderShippingFee = 0,
            TotalAmount = 0,
            CustomerNote = null,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            Items = new List<GetCustomerCartItemsResponse>(),
            AppliedPromotions = new List<GetCustomerCartAppliedPromotionsResponse>()
        };

        await _redisService.SetHashAsync(
            hashKey, BuildCartField(newCartId), JsonSerializer.Serialize(newCart));

        metadata.CartCount = 1;
        metadata.ActiveCartId = newCartId;
        metadata.LastUpdated = DateTime.UtcNow;
        await _redisService.SetHashAsync(
            hashKey, MetadataField, JsonSerializer.Serialize(metadata));

        _logger.Information(
            "New cart {CartId} created for customer {CustomerId}", newCartId, customerId);

        return (newCart, null);
    }

    private async Task<CartMetadata> GetMetadata(string hashKey)
    {
        var metadataJson = await _redisService.GetHashAsync(hashKey, MetadataField);
        return string.IsNullOrEmpty(metadataJson)
            ? new CartMetadata()
            : JsonSerializer.Deserialize<CartMetadata>(metadataJson) ?? new CartMetadata();
    }

    private string BuildHashKey(Guid customerId) =>
        $"{CacheConfig.EntityListCachePrefix("carts")}:{customerId}";

    private string BuildCartField(Guid cartId) => $"cart:{cartId}";

    // =========================================================
    // PARSE HELPERS
    // =========================================================

    internal static List<Guid>? ParseGuidList(string? value)
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

    internal static (Guid Id, int Qty)? ParseIdColonQty(string? value)
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

// ─── Shared DTO ────────────────────────────────────────────────────────────

public record DiscountResult
{
    public bool IsApplicable { get; init; }
    public decimal DiscountAmount { get; init; }
    public EPromotionType PromotionType { get; init; }
    public string? ErrorMessage { get; init; }

    public static DiscountResult Invalid(string error) => new()
    {
        IsApplicable = false,
        ErrorMessage = error
    };
}