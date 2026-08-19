using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPublicBrandPaymentMethods;

public class GetPublicBrandPaymentMethodsQueryHanlder : IRequestHandler<GetPublicBrandPaymentMethodsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetPublicBrandPaymentMethodsQueryHanlder(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetPublicBrandPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BrandCode))
        {
            throw new BadHttpRequestException("Mã thương hiệu không được để trống!");
        }

        var cacheKey = BuildCacheKey(request);
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult =
                    JsonSerializer.Deserialize<ICollection<GetPublicBrandPaymentMethodResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách phương thức thanh toán thành công!",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var brandPublicPaymentMethods = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .GetListAsync<GetPublicBrandPaymentMethodResponse>(
                predicate: x => (
                    x.Brand.Code == request.BrandCode
                    && x.Brand.Status == EBrandStatus.Active
                    && x.IsActive == true
                    && x.PaymentMethods.Status == EPaymentMethodStatus.Active),
                include: x => x.Include(x => x.Brand)
                    .Include(x => x.PaymentMethods),
                orderBy: x => x.OrderBy(x => x.DisplayOrder)
            );
        foreach (var paymentMethod in brandPublicPaymentMethods)
        {
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
        }

        try
        {
            var serializedData = JsonSerializer.Serialize(brandPublicPaymentMethods);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PublicPaymentMethodsCacheTTL
            );
            _logger.Information(
                $"Cached payment methods list with key: {cacheKey}, TTL: {CacheConfig.PublicPaymentMethodsCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache payment methods list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách phương thức thanh toán thành công!",
            Data = brandPublicPaymentMethods
        };
    }

    private string BuildCacheKey(GetPublicBrandPaymentMethodsQuery request)
    {
        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.BrandPaymentMethods)}:{ERole.BrandAdmin}:{request.BrandCode}")}:public";
    }
}