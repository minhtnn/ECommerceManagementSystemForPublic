using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Orders;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;
    private readonly IMapper _mapper;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;


    public GetOrderByIdQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IRedisService redisService,
        ICacheInvalidationService cacheService,
        IClaimService claimService,
        IMapper mapper, IPaymentGatewayFactory paymentGatewayFactory)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _cacheService = cacheService;
        _claimService = claimService;
        _mapper = mapper;
        _paymentGatewayFactory = paymentGatewayFactory;
    }

    public async ValueTask<ApiResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Get current user info
        var role = _claimService.GetCurrentRoleEnum();
        var referenceId = _claimService.GetCurrentReferenceId(); // CustomerId or BrandId

        if (role == null || referenceId == null || referenceId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        // 2. Try get from cache
        try
        {
            var cachedOrder = await _cacheService.GetDetailFromCacheAsync<GetOrderByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Orders), request.OrderId.ToString())}:{role}:{referenceId}"
            );

            if (cachedOrder != null)
            {
                var hasAccess = await VerifyOrderAccess(request.OrderId, role, referenceId);
                if (!hasAccess)
                {
                    return new ApiResponse()
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Message = "Bạn không có quyền truy cập đơn hàng này!"
                    };
                }

                cachedOrder.CreatedDate = TimeUtil.ConvertFromUtc(cachedOrder.CreatedDate, request.TimeZone);
                cachedOrder.LastModifiedDate =
                    TimeUtil.ConvertFromUtc(cachedOrder.LastModifiedDate ?? cachedOrder.CreatedDate, request.TimeZone);
                foreach (var orderPaymentResponse in cachedOrder.Payments)
                {
                    if (orderPaymentResponse.PaidAt.HasValue)
                    {
                        orderPaymentResponse.PaidAt =
                            TimeUtil.ConvertFromUtc(orderPaymentResponse.PaidAt.Value, request.TimeZone);
                    }
                }

                _logger.Debug($"Cache HIT for order:{request.OrderId}");

                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin đơn hàng thành công",
                    Data = cachedOrder
                };
            }

            _logger.Debug($"Cache MISS for order:{request.OrderId}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        // 3. Get order from database with includes
        var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.OrderId,
                include: i => i
                    .Include(x => x.OrderDetails)
                    .Include(x => x.Customer)
                    .ThenInclude(c => c.Brand)
                    .Include(x => x.Payments)
                    .Include(x => x.AppliedOrderPromotions)
            );

        if (order == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy đơn hàng!"
            };
        }

        // 4. ⭐ CRITICAL: Authorization check based on role
        var authorized = false;
        if (role == ERole.EndCustomer)
        {
            // Customer can only see their own orders
            authorized = order.CustomerId == referenceId;
        }
        else if (role == ERole.BrandAdmin)
        {
            // BrandAdmin can only see orders from their brand's customers
            authorized = (order.Customer?.BrandId == referenceId && order.Customer.Brand.Status == EBrandStatus.Active);
        }
        else if (role == ERole.SystemAdmin)
        {
            // SystemAdmin can see all orders
            authorized = true;
        }

        if (!authorized)
        {
            _logger.Warning(
                "Unauthorized order access attempt: OrderId={OrderId}, Role={Role}, ReferenceId={ReferenceId}",
                request.OrderId, role, referenceId);

            return new ApiResponse()
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn không có quyền truy cập đơn hàng này!"
            };
        }

        // 5. Map to response
        var orderResponse = _mapper.Map<GetOrderByIdResponse>(order);
        if (order.OrderStatus == EOrderStatus.WaitingPayment &&
            order.PaymentStatus == EPaymentStatus.Pending)
        {
            var payment = order.Payments?.FirstOrDefault();
            if (payment != null && payment.PaymentMethod != null)
            {
                try
                {
                    // Get BrandPaymentMethod for configuration
                    var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                        .SingleOrDefaultAsync(
                            predicate: x => x.PaymentMethodId == payment.PaymentMethodId,
                            include: i => i.Include(x => x.PaymentMethods)
                        );

                    if (brandPaymentMethod != null &&
                        brandPaymentMethod.PaymentMethods?.Code != "COD")
                    {
                        // Re-generate payment info (QR code, payment URL)
                        var paymentGateway = _paymentGatewayFactory.GetGateway(
                            brandPaymentMethod.PaymentMethods.Code);

                        var paymentResult = await paymentGateway.CreatePaymentAsync(
                            order,
                            payment,
                            brandPaymentMethod.Configuration ?? "{}",
                            cancellationToken);

                        if (paymentResult.Success)
                        {
                            orderResponse.PaymentUrl = paymentResult.PaymentUrl;
                            orderResponse.QrCode = paymentResult.QrCode;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to generate payment info for order {OrderId}", order.Id);
                    // Don't fail the request, just log the warning
                }
            }
        }

        // 6. Cache the result
        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Orders), order.Id.ToString())}:{role}:{referenceId}",
                orderResponse,
                TimeSpan.FromMinutes(5)
            );
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache order {OrderId}: {Error}", request.OrderId, e.Message);
        }

        if (role == ERole.BrandAdmin)
        {
            orderResponse.QrCode = null;
            orderResponse.PaymentUrl = null;
        }

        orderResponse.CreatedDate = TimeUtil.ConvertFromUtc(orderResponse.CreatedDate, request.TimeZone);
        orderResponse.LastModifiedDate =
            TimeUtil.ConvertFromUtc(orderResponse.LastModifiedDate ?? orderResponse.CreatedDate, request.TimeZone);
        foreach (var orderPaymentResponse in orderResponse.Payments)
        {
            if (orderPaymentResponse.PaidAt.HasValue)
            {
                orderPaymentResponse.PaidAt =
                    TimeUtil.ConvertFromUtc(orderPaymentResponse.PaidAt.Value, request.TimeZone);
            }
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin đơn hàng thành công",
            Data = orderResponse
        };
    }

    /// <summary>
    /// Verify if current user has access to the order
    /// </summary>
    private async Task<bool> VerifyOrderAccess(Guid orderId, ERole role, Guid referenceId)
    {
        var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == orderId,
                include: i => i.Include(x => x.Customer)
            );

        if (order == null) return false;

        return role switch
        {
            ERole.EndCustomer => order.CustomerId == referenceId,
            ERole.BrandAdmin => order.Customer?.BrandId == referenceId,
            ERole.SystemAdmin => true,
            _ => false
        };
    }
}