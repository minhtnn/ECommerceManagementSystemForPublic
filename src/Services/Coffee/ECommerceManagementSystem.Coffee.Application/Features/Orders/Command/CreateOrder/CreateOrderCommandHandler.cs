using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Orders;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly IRedisService _redisService;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IMediaService _mediaService;
    private readonly IEmailService _emailService;
    private const string MetadataField = "metadata";

    public CreateOrderCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        ILogger logger,
        IClaimService claimService,
        IRedisService redisService,
        ICacheInvalidationService cacheService, IMediaService mediaService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _logger = logger;
        _claimService = claimService;
        _redisService = redisService;
        _cacheService = cacheService;
        _mediaService = mediaService;
        _emailService = emailService;
    }

    public async ValueTask<ApiResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        #region Get claims

        var role = _claimService.GetCurrentRoleEnum();
        var customerId = _claimService.GetCurrentReferenceId();
        if (role == null || role != ERole.EndCustomer || customerId == null || customerId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        #endregion

        #region Start transaction

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Cannot begin transaction: {Message}", transactionResult.Message);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        #endregion

        #region Check logic before creating order

        var customer = await _unitOfWork.GetRepository<Domain.Entities.Customers>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == customerId,
                include: x => x.Include(x => x.Brand)
            );

        if (customer == null)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Khách hàng không tồn tại!"
            };
        }

        var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.BrandPaymentMethodId && x.IsActive,
                include: i => i.Include(x => x.PaymentMethods)
            );

        if (brandPaymentMethod == null)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Phương thức thanh toán không tồn tại hoặc đã bị vô hiệu hóa!"
            };
        }

        if (brandPaymentMethod.PaymentMethods?.Status != EPaymentMethodStatus.Active)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Phương thức thanh toán hiện không khả dụng!"
            };
        }

        #endregion

        #region Get customer cart information

        var hashKey = BuildHashKey(customerId);
        var cartField = BuildCartField(request.CartId);
        var cartJson = await _redisService.GetHashAsync(hashKey, cartField);
        if (string.IsNullOrEmpty(cartJson))
        {
            _logger.Warning(
                "Cart {CartId} referenced in metadata not found for customer {CustomerId}",
                request.CartId, customerId);
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy giỏ hàng. Vui lòng tạo giỏ hàng mới!"
            };
        }

        var cart = JsonSerializer.Deserialize<GetCustomerCartResponse>(cartJson);
        if (cart == null)
        {
            _logger.Error("Failed to deserialize cart {CartId}", request.CartId);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Lỗi xử lý dữ liệu giỏ hàng!"
            };
        }

        // ── Tách gift items và non-gift items ──────────────────────────────────
        var nonGiftItems = cart.Items?.Where(i => !i.IsGiftItem).ToList() ?? [];
        var giftItems = cart.Items?.Where(i => i.IsGiftItem).ToList() ?? [];

        if (nonGiftItems.Count == 0)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Giỏ hàng trống!"
            };
        }

        // ── Chỉ load products cho non-gift items (cần check stock) ────────────
        var nonGiftProductIds = nonGiftItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetListAsync(
                predicate: x => nonGiftProductIds.Contains(x.Id),
                include: x => x.Include(x => x.ProductImages)
            );

        if (products.Count != nonGiftProductIds.Count)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Một số sản phẩm không tồn tại!"
            };
        }

        #endregion

        #region Create order and relevant information

        // ── Tính totals — chỉ từ non-gift items ───────────────────────────────
        decimal totalAmountWithoutDiscount = 0;
        var orderDetails = new List<OrderDetails>();

        foreach (var item in nonGiftItems)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (product.StockQuantity < item.Quantity)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = $"Sản phẩm '{product.Name}' không đủ số lượng trong kho!"
                };
            }

            var lineTotal = (product.Price ?? 0) * item.Quantity;
            totalAmountWithoutDiscount += lineTotal;

            orderDetails.Add(new OrderDetails
            {
                Id = Guid.CreateVersion7(),
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                Quantity = item.Quantity,
                UnitPriceSnapshot = product.Price ?? 0,
                TotalPriceSnapshot = lineTotal,
                IsGiftItem = false,
                GiftFromPromotionId = null,
                // Product = product
            });
        }

        // ── Thêm gift items vào OrderDetails với giá = 0 ──────────────────────
        foreach (var item in giftItems)
        {
            orderDetails.Add(new OrderDetails
            {
                Id = Guid.CreateVersion7(),
                ProductId = item.ProductId,
                ProductNameSnapshot = item.ProductNameSnapshot,
                Quantity = item.Quantity,
                UnitPriceSnapshot = 0,
                TotalPriceSnapshot = 0,
                IsGiftItem = true,
                GiftFromPromotionId = item.PromotionId,
            });
        }

        // ── Tính discount và total ─────────────────────────────────────────────
        var totalOrderDiscount = cart.AppliedPromotions?.Sum(p => p.DiscountAmountApplied) ?? 0;
        totalOrderDiscount = Math.Min(totalOrderDiscount, totalAmountWithoutDiscount);
        totalOrderDiscount = Math.Max(0, totalOrderDiscount);

        var totalAmount = Math.Max(0, totalAmountWithoutDiscount - totalOrderDiscount + cart.TotalOrderShippingFee);

        // ── Tạo Order ─────────────────────────────────────────────────────────
        var orderCode = await GenerateOrderCode();
        var order = new Domain.Entities.Orders
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            Code = orderCode,
            PaymentStatus = EPaymentStatus.Pending,
            TotalAmountWithoutDiscount = totalAmountWithoutDiscount,
            TotalOrderDiscount = totalOrderDiscount,
            TotalOrderShippingFee = cart.TotalOrderShippingFee,
            TotalAmount = totalAmount,
            ShippingAddress = request.ShippingAddress,
            ShippingContact = request.ShippingContact,
            CustomerNote = request.CustomerNote,
            CreatedDate = DateTime.UtcNow
        };

        // ── Insert OrderDetails ────────────────────────────────────────────────
        foreach (var detail in orderDetails)
        {
            detail.OrderId = order.Id;
            await _unitOfWork.GetRepository<OrderDetails>().InsertAsync(detail);
        }

        order.OrderDetails = orderDetails;

        // ── Lưu AppliedOrderPromotions ────────────────────────────────────────
        if (cart.AppliedPromotions?.Count > 0)
        {
            var appliedOrderPromotions = cart.AppliedPromotions
                .Select((p, index) => new AppliedOrderPromotions
                {
                    Id = Guid.CreateVersion7(),
                    OrderId = order.Id,
                    PromotionRuleId = p.PromotionId,
                    PromotionRuleNameSnapshot = p.PromotionRuleNameSnapshot,
                    DiscountAmountApplied = p.DiscountAmountApplied,
                    StackingSlot = p.StackingSlot,
                    ApplyOrder = index + 1,
                })
                .ToList();

            foreach (var promo in appliedOrderPromotions)
            {
                await _unitOfWork.GetRepository<AppliedOrderPromotions>().InsertAsync(promo);
            }

            order.AppliedOrderPromotions = appliedOrderPromotions;
        }

        #endregion

        #region Create order payment

        var payment = new Domain.Entities.Payments
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            PaymentMethodId = brandPaymentMethod.PaymentMethodId,
            PaymentMethodCodeSnapshot = brandPaymentMethod.PaymentMethods.Code,
            Amount = order.TotalAmount,
            PaymentStatus = EPaymentStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        var paymentCode = brandPaymentMethod.PaymentMethods.Code;
        var paymentGateway = _paymentGatewayFactory.GetGateway(paymentCode);

        var paymentResult = await paymentGateway.CreatePaymentAsync(
            order,
            payment,
            brandPaymentMethod.Configuration ?? "{}",
            cancellationToken);

        if (!paymentResult.Success)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error("Payment creation failed: {Error}", paymentResult.ErrorMessage);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = $"Không thể tạo thanh toán: {paymentResult.ErrorMessage}"
            };
        }

        if (!string.IsNullOrEmpty(paymentResult.TransactionId))
            payment.TransactionId = paymentResult.TransactionId;

        if (paymentResult.Success)
        {
            order.PaymentUrl = paymentResult.PaymentUrl;
            order.QrCode = paymentResult.QrCode;
            order.OrderStatus = (paymentCode.ToUpperInvariant().Equals("PAYINCASH"))
                ? EOrderStatus.Pending
                : EOrderStatus.WaitingPayment;
        }

        #endregion

        #region Update stock — chỉ trừ non-gift items

        foreach (var item in nonGiftItems)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.StockQuantity -= item.Quantity;
        }

        _unitOfWork.GetRepository<Domain.Entities.Products>().UpdateRange(products);

        #endregion

        await _unitOfWork.GetRepository<Domain.Entities.Orders>().InsertAsync(order);
        await _unitOfWork.GetRepository<Domain.Entities.Payments>().InsertAsync(payment);
        order.Customer = customer;

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error("Transaction commit failed: {Message}", commitResult.Message);
            throw new Exception($"Không thể tạo đơn hàng: {commitResult.Message}");
        }

        if (order.OrderStatus == EOrderStatus.Pending)
        {
            var brandSetting = SettingUtil.Parse<BrandSetting>(customer.Brand?.Configuration);

            if (!brandSetting.EnabledSendEmailFunction)
            {
                throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
            }

            if (string.IsNullOrWhiteSpace(brandSetting.SendGridApiKey) ||
                string.IsNullOrWhiteSpace(brandSetting.SendGridFromEmail) ||
                string.IsNullOrWhiteSpace(brandSetting.SendGridFromName))
            {
                throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin gửi email!");
            }

            string logoBase64String = null;
            if (!string.IsNullOrWhiteSpace(customer.Brand?.LogoUrl))
            {
                try
                {
                    logoBase64String = await _mediaService.GetImageUrlAsync(
                        customer.Brand.LogoUrl,
                        TimeSpan.FromHours(2)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to get image url", ex);
                }
            }

            var sendConfirmOrderEmailRequest = new SendConfirmOrderEmailRequest()
            {
                BrandLogoBase64 = logoBase64String ?? "",
                BrandName = customer.Brand?.Name ?? "",
                CustomerName = customer.FullName,
                FromEmail = customer.Brand?.Email,
                CustomerEmail = customer.Email,
                ReceiveNumber = order.ShippingContact,
                ReceiveAddress = order.ShippingAddress,
                OrderDate = order.CreatedDate,
                OrderCode = order.Code,
                SubTotal = order.TotalAmountWithoutDiscount,
                DiscountAmount = order.TotalOrderDiscount,
                ShippingAmount = order.TotalOrderShippingFee,
                TotalAmount = order.TotalAmount,
                OrderDetails = order.OrderDetails.Select(x =>
                {
                    var product = products.FirstOrDefault(p => p.Id == x.ProductId);
                    return new SendConfirmOrderDetailEmailRequest()
                    {
                        ProductName = x.ProductNameSnapshot ?? "",
                        Quantity = x.Quantity,
                        ProductImagePath = x.IsGiftItem
                            ? null
                            :
                            (product != null && product.ProductImages.Any())
                                ?
                                product?.ProductImages.FirstOrDefault(x => x.IsMainImage).ImageUrl
                                :
                                null,
                        IsGiftItem = x.IsGiftItem,
                        UnitPriceSnapshot = x.UnitPriceSnapshot,
                        TotalPriceSnapshot = x.TotalPriceSnapshot,
                    };
                }).ToList(),
            };
            foreach (var x in sendConfirmOrderEmailRequest.OrderDetails)
            {
                if (!string.IsNullOrWhiteSpace(x.ProductImagePath))
                {
                    try
                    {
                        x.ProductImageBase64 = await _mediaService.GetImageUrlAsync(
                            x.ProductImagePath,
                            TimeSpan.FromHours(2)
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to get image url", ex);
                    }
                }
            }

            var emailResult = await _emailService.SendOrderConfirmationAsync(
                brandSetting.SendGridApiKey,
                brandSetting.SendGridFromEmail,
                brandSetting.SendGridFromName,
                brandSetting.MainColor,
                sendConfirmOrderEmailRequest,
                cancellationToken
            );

            if (!emailResult.IsSuccess)
            {
                _logger.Warning(
                    "Account created but email sending failed: {Error}",
                    emailResult.Message
                );
            }
        }

        #region Clear cart & invalidate cache (post-commit, non-critical)

        try
        {
            await _redisService.RemoveHashAsync(hashKey, cartField);

            var metadata = await GetMetadata(hashKey);
            metadata.CartCount = Math.Max(0, metadata.CartCount - 1);
            if (metadata.ActiveCartId == request.CartId)
                metadata.ActiveCartId = null;

            await _redisService.SetHashAsync(hashKey, MetadataField, JsonSerializer.Serialize(metadata));

            _logger.Information("Cart {CartId} cleared after order {OrderCode}", request.CartId, orderCode);

            await _cacheService.InvalidateEntityCacheAsync(
                lockKey: CacheConfig.EntityInvalidationLock(
                    CacheConfig.EntityListCachePrefix(
                        $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{customerId}")),
                operation: EOperationBeforeCache.BulkCreate,
                counterKey: CacheConfig.EntityInvalidationCounter(
                    CacheConfig.EntityListCachePrefix(
                        $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{customerId}")),
                entityCachePrefix: CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{customerId}")
            );

            _logger.Information("Cache invalidated after order creation: OrderCode={OrderCode}", orderCode);
        }
        catch (Exception ex)
        {
            // Không fail order nếu clear cache thất bại
            _logger.Warning(ex, "Failed to clear cart/cache after order creation");
        }

        #endregion

        _logger.Information("Order created successfully: {OrderCode}", orderCode);

        return new ApiResponse
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo đơn hàng thành công!",
            Data = new CreateOrderResponse
            {
                OrderId = order.Id,
                OrderCode = order.Code,
                TotalAmount = order.TotalAmount,
                PaymentUrl = paymentResult.PaymentUrl,
                QrCode = paymentResult.QrCode,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedDate = TimeUtil.ConvertFromUtc(order.CreatedDate, request.TimeZone)
            }
        };
    }

    private async Task<string> GenerateOrderCode()
    {
        const int maxRetries = 10;
        for (int i = 0; i < maxRetries; i++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10_000_000_000L;
            var random = Random.Shared.Next(100000, 999999);
            var code = $"ORD{timestamp}{random}";

            var exists = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
                .SingleOrDefaultAsync(predicate: x => x.Code == code);

            if (exists == null) return code;
            await Task.Delay(10);
        }

        throw new InvalidOperationException("Không thể tạo mã đơn hàng sau nhiều lần thử!");
    }

    private async Task<CartMetadata> GetMetadata(string hashKey)
    {
        var metadataJson = await _redisService.GetHashAsync(hashKey, MetadataField);
        if (string.IsNullOrEmpty(metadataJson)) return new CartMetadata();
        return JsonSerializer.Deserialize<CartMetadata>(metadataJson) ?? new CartMetadata();
    }

    private string BuildHashKey(Guid customerId) =>
        $"{CacheConfig.EntityListCachePrefix("carts")}:{customerId}";

    private string BuildCartField(Guid cartId) => $"cart:{cartId}";
}