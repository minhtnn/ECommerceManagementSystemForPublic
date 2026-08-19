using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.HandlePaymentCallback;

public class HandlePaymentCallbackCommandHandler : IRequestHandler<HandlePaymentCallbackCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IEmailService _emailService;
    private readonly IMediaService _mediaService;

    public HandlePaymentCallbackCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        ILogger logger, ICacheInvalidationService cacheInvalidation, IEmailService emailService,
        IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _logger = logger;
        _cacheInvalidation = cacheInvalidation;
        _emailService = emailService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(
        HandlePaymentCallbackCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CallbackData == null)
        {
            _logger.Warning("Received null callback data");
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Dữ liệu callback không hợp lệ"
            };
        }

        _logger.Information(
            "Received {Gateway} callback: Code={Code}, PaymentLinkId={PaymentLinkId}, OrderCode={OrderCode}",
            request.PaymentGatewayCode,
            request.CallbackData.Data?.Code,
            request.CallbackData.Data?.PaymentLinkId,
            request.CallbackData.Data?.OrderCode);

        try
        {
            var transactionId = request.CallbackData.Data?.PaymentLinkId;

            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.Warning("Missing PaymentLinkId in callback");
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Thiếu mã giao dịch trong callback"
                };
            }

            // Find payment by transaction ID
            var payment = await _unitOfWork.GetRepository<Domain.Entities.Payments>()
                .SingleOrDefaultAsync(
                    predicate: x => x.TransactionId == transactionId,
                    include: i => i.Include(x => x.Order)
                        .ThenInclude(o => o.OrderDetails)
                        .Include(x => x.Order).ThenInclude(x => x.OrderDetails).ThenInclude(x => x.Product).ThenInclude(x => x.ProductImages)
                        .Include(x => x.Order).ThenInclude(o => o.Customer)
                );

            if (payment == null)
            {
                _logger.Warning(
                    "Payment not found for transaction: {TransactionId} - Likely a test webhook from PayOS",
                    transactionId);

                // Return 200 OK so PayOS knows webhook URL is working
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Webhook received - Payment not found (test webhook or invalid transaction)",
                    Data = new
                    {
                        TransactionId = transactionId,
                        OrderCode = request.CallbackData.Data?.OrderCode,
                        IsTestWebhook = true
                    }
                };
            }

            _logger.Information(
                "Found payment: {PaymentId}, Order: {OrderCode}, CurrentStatus: {PaymentStatus}",
                payment.Id,
                payment.Order.Code,
                payment.PaymentStatus);

            // Skip if already processed
            if (payment.PaymentStatus == EPaymentStatus.Completed)
            {
                _logger.Information(
                    "Payment already completed - OrderCode: {OrderCode}",
                    payment.Order.Code);

                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Payment already processed",
                    Data = new
                    {
                        OrderCode = payment.Order.Code,
                        PaymentStatus = payment.PaymentStatus
                    }
                };
            }

            // Get BrandPaymentMethod with configuration
            var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                .SingleOrDefaultAsync(
                    predicate: x => x.PaymentMethodId == payment.PaymentMethodId,
                    include: i => i.Include(x => x.PaymentMethods)
                        .Include(x => x.Brand)
                );

            if (brandPaymentMethod == null)
            {
                _logger.Error(
                    "BrandPaymentMethod not found for PaymentMethodId: {PaymentMethodId}",
                    payment.PaymentMethodId);

                return new ApiResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy cấu hình phương thức thanh toán"
                };
            }

            // Resolve payment gateway using factory
            var gateway = _paymentGatewayFactory.GetGatewayByBrandPaymentMethod(brandPaymentMethod);

            _logger.Information(
                "🔧 Resolved gateway: {GatewayCode} for BrandPaymentMethod: {BrandPaymentMethodId}",
                gateway.Code,
                brandPaymentMethod.Id);

            // Verify payment callback
            var verificationResult = await gateway.VerifyPaymentAsync(
                request.CallbackData,
                brandPaymentMethod.Configuration ?? "{}",
                cancellationToken);

            _logger.Information(
                "Verification result - IsValid: {IsValid}, IsSuccess: {IsSuccess}",
                verificationResult.IsValid,
                verificationResult.IsSuccess);

            if (!verificationResult.IsValid)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Chữ ký callback không hợp lệ"
                };
            }

            // Begin transaction to update payment and order
            var transactionResult = await _unitOfWork.BeginTransactionAsync();
            if (!transactionResult.IsSuccess)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = "Không thể bắt đầu xử lý thanh toán"
                };
            }

            try
            {
                var previousOrderStatus = payment.Order.OrderStatus;

                if (verificationResult.IsSuccess)
                {
                    payment.PaymentStatus = EPaymentStatus.Completed;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.Order.PaymentStatus = EPaymentStatus.Completed;
                    payment.Order.OrderStatus = EOrderStatus.Pending;
                    if (payment.Order.OrderStatus == EOrderStatus.Pending)
                    {
                        var brandSetting = SettingUtil.Parse<BrandSetting>(brandPaymentMethod.Brand?.Configuration);

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
                        if (!string.IsNullOrWhiteSpace(brandPaymentMethod.Brand?.LogoUrl))
                        {
                            try
                            {
                                logoBase64String = await _mediaService.GetImageUrlAsync(
                                    brandPaymentMethod.Brand.LogoUrl,
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
                            BrandName = brandPaymentMethod.Brand?.Name ?? "",
                            CustomerName = payment.Order.Customer?.FullName ?? "",
                            FromEmail = brandPaymentMethod.Brand?.Email,
                            CustomerEmail = payment.Order.Customer?.Email ?? "",
                            ReceiveNumber = payment.Order.ShippingContact,
                            ReceiveAddress = payment.Order.ShippingAddress,
                            OrderDate = payment.Order.CreatedDate,
                            OrderCode = payment.Order.Code,
                            SubTotal = payment.Order.TotalAmountWithoutDiscount,
                            DiscountAmount = payment.Order.TotalOrderDiscount,
                            ShippingAmount = payment.Order.TotalOrderShippingFee,
                            TotalAmount = payment.Order.TotalAmount,
                            OrderDetails = payment.Order.OrderDetails.Select(x =>
                                new SendConfirmOrderDetailEmailRequest()
                                {
                                    ProductName = x.ProductNameSnapshot ?? "",
                                    Quantity = x.Quantity,
                                    ProductImagePath = x.IsGiftItem ? null :
                                        (x.Product != null && x.Product.ProductImages.Any()) ? x.Product?.ProductImages
                                            .FirstOrDefault(x => x.IsMainImage).ImageUrl :
                                        null,
                                    IsGiftItem = x.IsGiftItem,
                                    UnitPriceSnapshot = x.UnitPriceSnapshot,
                                    TotalPriceSnapshot = x.TotalPriceSnapshot,
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

                    _logger.Information(
                        "Payment completed - Order: {OrderCode}, Amount: {Amount}",
                        payment.Order.Code,
                        verificationResult.Amount);
                }
                else
                {
                    // Payment failed
                    _logger.Warning("Payment FAILED - Updating status");

                    payment.PaymentStatus = EPaymentStatus.Failed;
                    payment.FailedAt = DateTime.UtcNow;
                    payment.FailedReason = verificationResult.ErrorMessage ?? "Payment failed";
                    payment.Order.PaymentStatus = EPaymentStatus.Failed;
                    payment.Order.OrderStatus = EOrderStatus.Cancelled;

                    _logger.Warning(
                        "Payment failed - Order: {OrderCode}, Reason: {Reason}",
                        payment.Order.Code,
                        payment.FailedReason);

                    // Restore product stock
                    foreach (var detail in payment.Order.OrderDetails)
                    {
                        var product = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                            .SingleOrDefaultAsync(predicate: x => x.Id == detail.ProductId);

                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                            _unitOfWork.GetRepository<Domain.Entities.Products>().UpdateAsync(product);

                            _logger.Information(
                                "Restored stock - Product: {ProductId}, Quantity: +{Quantity}",
                                product.Id,
                                detail.Quantity);
                        }
                    }
                }

                // Save gateway response for audit
                payment.GateWayResponse = JsonSerializer.Serialize(request.CallbackData);
                payment.LastModifiedDate = DateTime.UtcNow;

                _unitOfWork.GetRepository<Domain.Entities.Payments>().UpdateAsync(payment);
                _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(payment.Order);

                // Create order history
                var orderHistory = new OrderHistoryStatus
                {
                    Id = Guid.CreateVersion7(),
                    OrderId = payment.Order.Id,
                    FromStatus = previousOrderStatus,
                    ToStatus = payment.Order.OrderStatus,
                    Note = verificationResult.IsSuccess
                        ? $"Thanh toán thành công - {request.CallbackData.Data?.Description}"
                        : $"Thanh toán thất bại: {verificationResult.ErrorMessage}",
                    LastModifiedDate = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<OrderHistoryStatus>().InsertAsync(orderHistory);

                // Commit transaction
                var commitResult = await _unitOfWork.CommitTransactionAsync();
                if (!commitResult.IsSuccess)
                {
                    _logger.Error("Commit failed: {Message}", commitResult.Message);
                    throw new Exception($"Không thể lưu kết quả thanh toán: {commitResult.Message}");
                }

                _logger.Information(
                    "🎉 Payment callback processed successfully - Order: {OrderCode}, Status: {OrderStatus}",
                    payment.Order.Code,
                    payment.Order.OrderStatus);
                try
                {
                    // Invalidate order detail cache
                    var cacheBrandListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                        lockKey: CacheConfig.EntityInvalidationLock(
                            CacheConfig.EntityListCachePrefix(
                                $"{nameof(Domain.Entities.Orders)}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}")
                        ),
                        operation: EOperationBeforeCache.BulkUpdate,
                        counterKey: CacheConfig.EntityInvalidationCounter(
                            CacheConfig.EntityListCachePrefix(
                                $"{nameof(Domain.Entities.Orders)}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}")
                        ),
                        entityCachePrefix:
                        CacheConfig.EntityListCachePrefix(
                            $"{nameof(Domain.Entities.Orders)}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}")
                    );

                    var cacheBrandByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                        lockKey: CacheConfig.EntityInvalidationLock(
                            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}"
                        ),
                        operation: EOperationBeforeCache.BulkUpdate,
                        counterKey: CacheConfig.EntityInvalidationCounter(
                            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}"
                        ),
                        entityCachePrefix:
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.BrandAdmin}:{payment.Order.Customer.BrandId}"
                    );
                    var cacheCustomerListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                        lockKey: CacheConfig.EntityInvalidationLock(
                            CacheConfig.EntityListCachePrefix(
                                $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{payment.Order.Customer.BrandId}")
                        ),
                        operation: EOperationBeforeCache.BulkUpdate,
                        counterKey: CacheConfig.EntityInvalidationCounter(
                            CacheConfig.EntityListCachePrefix(
                                $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{payment.Order.Customer.BrandId}")
                        ),
                        entityCachePrefix:
                        CacheConfig.EntityListCachePrefix(
                            $"{nameof(Domain.Entities.Orders)}:{ERole.EndCustomer}:{payment.Order.Customer.BrandId}")
                    );

                    var cacheCustomerByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                        lockKey: CacheConfig.EntityInvalidationLock(
                            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.EndCustomer}:{payment.Order.CustomerId}"
                        ),
                        operation: EOperationBeforeCache.BulkUpdate,
                        counterKey: CacheConfig.EntityInvalidationCounter(
                            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.EndCustomer}:{payment.Order.CustomerId}"
                        ),
                        entityCachePrefix:
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), payment.OrderId.ToString())}:{ERole.EndCustomer}:{payment.Order.CustomerId}"
                    );
                    if (cacheBrandListResult.Success && cacheBrandByIdResult.Success &&
                        cacheCustomerByIdResult.Success && cacheCustomerListResult.Success)
                    {
                        _logger.Information(
                            $"Updated orders '{payment.Order.Code}' (ID: {payment.Order.Id}). Cache: {cacheBrandListResult.Message}, {cacheBrandByIdResult.Message}, {cacheCustomerListResult.Message}, {cacheCustomerByIdResult.Message}."
                        );
                    }
                    else
                    {
                        _logger.Warning(
                            $"Updated brand '{payment.Order.Code}' but cache invalidation failed: {cacheBrandListResult.Message}, {cacheBrandByIdResult.Message}, {cacheCustomerListResult.Message}, {cacheCustomerByIdResult.Message}."
                        );
                    }


                    _logger.Information("Cache invalidated after payment callback: OrderId={OrderId}",
                        payment.Order.Id);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to invalidate cache after payment callback");
                    // Don't fail the request
                }

                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = verificationResult.IsSuccess
                        ? "Xử lý thanh toán thành công"
                        : "Thanh toán thất bại",
                    Data = new
                    {
                        OrderCode = payment.Order.Code,
                        PaymentStatus = payment.PaymentStatus.ToString(),
                        OrderStatus = payment.Order.OrderStatus.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.Error(ex, "Error processing payment callback");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fatal error in callback handler");
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Lỗi xử lý callback thanh toán"
            };
        }
    }
}