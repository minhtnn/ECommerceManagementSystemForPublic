using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetBrandPaymentMethods;

public class GetBrandPaymentMethodsQueryHandler : IRequestHandler<GetBrandPaymentMethodsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetBrandPaymentMethodsQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IRedisService redisService,
        IClaimService claimService,
        IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
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

        var cacheKey = BuildCacheKey(request, brandId.ToString());

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetBrandPaymentMethodsResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách phương thức thanh toán của thương hiệu thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var brandPaymentMethods = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .GetPagingListAsync<GetBrandPaymentMethodsResponse>(
                predicate: x => (
                    x.PaymentMethods != null &&
                    (string.IsNullOrEmpty(request.Code) || x.PaymentMethods.Code.Contains(request.Code))
                    && (string.IsNullOrEmpty(request.Name) || x.PaymentMethods.Name.Contains(request.Name))
                    && (request.Status == null || x.IsActive == request.Status)
                    && (x.BrandId == brandId) && (x.PaymentMethods.Status == EPaymentMethodStatus.Active)),
                include: x => x.Include(x => x.PaymentMethods),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "DisplayOrder",
                isAsc: request.IsAsc
            );

        #region Assign image

        foreach (var paymentMethod in brandPaymentMethods.Items)
        {
            string imageUrl = string.Empty;
            if (!string.IsNullOrEmpty(paymentMethod.ImagePath))
            {
                try
                {
                    imageUrl = await _mediaService.GetImageUrlAsync(
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

            paymentMethod.ImageUrl = imageUrl;
        }

        #endregion

        try
        {
            var serializedData = JsonSerializer.Serialize(brandPaymentMethods);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.PaymentMethodsCacheTTL
            );
            _logger.Information(
                $"Cached brand payment methods list with key: {cacheKey}, TTL: {CacheConfig.PaymentMethodsCacheTTL} minutes");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache brand payment methods list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách phương thức thanh toán của thương hiệu thành công",
            Data = brandPaymentMethods
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: brandpaymentmethods:list:{brandId}:{code}:{name}:{status}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetBrandPaymentMethodsQuery request, string brandId)
    {
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name.ToLowerInvariant();
        var code = string.IsNullOrEmpty(request.Code) ? "all" : request.Code.ToLowerInvariant();
        var status = request.Status == null ? "all" : request.Status.ToString()!.ToLowerInvariant();
        var sortBy = (request.SortBy ?? "DisplayOrder").ToLowerInvariant();

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId}")}:{code}:{name}:{status}:{request.Page}" +
            $":{request.Size}:{sortBy}:{request.IsAsc}";
    }
}