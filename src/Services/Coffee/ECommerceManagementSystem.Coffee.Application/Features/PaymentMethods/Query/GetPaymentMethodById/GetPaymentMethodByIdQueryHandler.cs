using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPaymentMethodById;

public class GetPaymentMethodByIdQueryHandler : IRequestHandler<GetPaymentMethodByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetPaymentMethodByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetPaymentMethodByIdQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        try
        {
            var cachedPaymentMethods = await _cacheService.GetDetailFromCacheAsync<GetPaymentMethodByIdResponse>(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.PaymentMethods),
                    request.PaymentMethodId.ToString())
            );

            if (cachedPaymentMethods != null)
            {
                _logger.Debug($"Cache HIT for brand:{request.PaymentMethodId}");
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin phương thức thanh toán thành công",
                    Data = cachedPaymentMethods
                };
            }

            _logger.Debug($"Cache MISS for brand:{request.PaymentMethodId}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var paymentMethod = await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>()
            .SingleOrDefaultAsync<GetPaymentMethodByIdResponse>(
                predicate: x => x.Id == request.PaymentMethodId
            );
        if (paymentMethod == null)
        {
            throw new BadHttpRequestException("Không tìm thấy phương thức thanh toán với ID đã cho");
        }

        if (paymentMethod.ImagePath != null && !string.IsNullOrEmpty(paymentMethod.ImagePath))
        {
            try
            {
                paymentMethod.ImageUrl = await _mediaService.GetImageUrlAsync(
                    paymentMethod.ImagePath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Failed to generate signed URL for image {ImageUrl}: {Error}",
                    paymentMethod.ImagePath,
                    ex.Message
                );
            }
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.PaymentMethods),
                    request.PaymentMethodId.ToString()), paymentMethod, CacheConfig.PaymentMethodsCacheTTL
            );
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache brand {BrandId}: {Error}", paymentMethod.Id, e.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin phương thức thanh toán thành công",
            Data = paymentMethod
        };
    }
}