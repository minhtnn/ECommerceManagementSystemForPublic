using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetBrandPaymentMethodById;

public class GetBrandPaymentMethodByIdQueryHandler : IRequestHandler<GetBrandPaymentMethodByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;
    private readonly IMapper _mapper;

    public GetBrandPaymentMethodByIdQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IRedisService redisService,
        IClaimService claimService,
        IMediaService mediaService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
        _mapper = mapper;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPaymentMethodByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authorization check
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

        // 2. Build cache key
        var cacheKey =
            $"{CacheConfig.EntityByIdCachePrefix(nameof(BrandPaymentMethods), request.Id.ToString())}:{ERole.BrandAdmin}:{brandId.ToString()}";

        // 3. Try get from cache
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<GetBrandPaymentMethodByIdResponse>(cachedData);
                if (cachedResult != null)
                {
                    cachedResult.CreatedDate = TimeUtil.ConvertFromUtc(cachedResult.CreatedDate, request.TimeZone);
                    cachedResult.LastModifiedDate =
                        TimeUtil.ConvertFromUtc(cachedResult.LastModifiedDate ?? cachedResult.CreatedDate,
                            request.TimeZone);
                }

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin phương thức thanh toán thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        // 4. Get from database
        var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .SingleOrDefaultAsync<GetBrandPaymentMethodByIdResponse>(
                predicate: x => x.Id == request.Id
                                && x.BrandId == brandId
                                && x.PaymentMethods.Status == EPaymentMethodStatus.Active
            );

        if (brandPaymentMethod == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy phương thức thanh toán của thương hiệu!"
            };
        }

        // 5. Process image URL
        if (!string.IsNullOrEmpty(brandPaymentMethod.ImagePath))
        {
            try
            {
                brandPaymentMethod.ImageUrl = await _mediaService.GetImageUrlAsync(
                    brandPaymentMethod.ImagePath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Failed to generate signed URL for image {ImageUrl}: {Error}",
                    brandPaymentMethod.ImagePath,
                    ex.Message
                );
            }
        }

        // 6. Cache the result
        try
        {
            var serializedData = JsonSerializer.Serialize(brandPaymentMethod);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PaymentMethodsCacheTTL
            );
            _logger.Information(
                $"Cached brand payment method detail with key: {cacheKey}, TTL: {CacheConfig.PaymentMethodsCacheTTL} minutes");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache brand payment method detail: {Error}", redisEx.Message);
        }

        if (brandPaymentMethod != null)
        {
            brandPaymentMethod.CreatedDate = TimeUtil.ConvertFromUtc(brandPaymentMethod.CreatedDate, request.TimeZone);
            brandPaymentMethod.LastModifiedDate =
                TimeUtil.ConvertFromUtc(brandPaymentMethod.LastModifiedDate ?? brandPaymentMethod.CreatedDate,
                    request.TimeZone);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin phương thức thanh toán thành công",
            Data = brandPaymentMethod
        };
    }
}