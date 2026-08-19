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
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetPaymentMethodsQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IRedisService redisService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin && role != ERole.BrandAdmin)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        if (role == ERole.SystemAdmin)
        {
            var cacheKey = BuildCacheKey(request);

            try
            {
                var cachedData = await _redisService.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.Debug($"Cache HIT: {cacheKey}");

                    var cachedResult = JsonSerializer.Deserialize<Paginate<GetPaymentMethodsResponse>>(cachedData);

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

            var paymentMethods = await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>()
                .GetPagingListAsync<GetPaymentMethodsResponse>(
                    predicate: x => ((string.IsNullOrEmpty(request.Code) || x.Code.Contains(request.Code))
                                     && (string.IsNullOrEmpty(request.Name) || x.Name.Contains(request.Name))
                                     && (request.Status == null || x.Status == request.Status)),
                    page: request.Page,
                    size: request.Size,
                    sortBy: request.SortBy ?? "CreatedDate",
                    isAsc: request.IsAsc
                );
            foreach (var paymentMethod in paymentMethods.Items)
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
                var serializedData = JsonSerializer.Serialize(paymentMethods);
                await _redisService.SetStringAsync(
                    cacheKey,
                    serializedData,
                    CacheConfig.PaymentMethodsCacheTTL
                );
                _logger.Information(
                    $"Cached payment methods list with key: {cacheKey}, TTL: {CacheConfig.PaymentMethodsCacheTTL}");
            }
            catch (RedisException redisEx)
            {
                _logger.Warning("Failed to cache payment methods list: {Error}", redisEx.Message);
            }

            return new ApiResponse()
            {
                Status = StatusCodes.Status200OK,
                Message = "Lấy danh sách phương thức thanh toán thành công!",
                Data = paymentMethods
            };
        }

        if (role == ERole.BrandAdmin)
        {
            var brandId = _claimService.GetCurrentReferenceId();
            var cacheKey = BuildCacheKey(request, role, brandId.ToString());

            try
            {
                var cachedData = await _redisService.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.Debug($"Cache HIT: {cacheKey}");

                    var cachedResult = JsonSerializer.Deserialize<Paginate<GetPaymentMethodsResponse>>(cachedData);

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

            var existingBrandPaymentMethods = (await _unitOfWork.GetRepository<BrandPaymentMethods>()
                .GetListAsync(
                    selector: x => x.PaymentMethodId,
                    predicate: x => x.BrandId == brandId
                )).ToList();
            var paymentMethods = await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>()
                .GetPagingListAsync<GetPaymentMethodsResponse>(
                    predicate: x => ((string.IsNullOrEmpty(request.Code) || x.Code.Contains(request.Code))
                                     && (string.IsNullOrEmpty(request.Name) || x.Name.Contains(request.Name))
                                     && (x.Status == EPaymentMethodStatus.Active) 
                                     && !(existingBrandPaymentMethods.Contains(x.Id))),
                    page: request.Page,
                    size: request.Size,
                    sortBy: request.SortBy ?? "CreatedDate",
                    isAsc: request.IsAsc
                );
            foreach (var paymentMethod in paymentMethods.Items)
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
                var serializedData = JsonSerializer.Serialize(paymentMethods);
                await _redisService.SetStringAsync(
                    cacheKey,
                    serializedData,
                    CacheConfig.PaymentMethodsCacheTTL
                );
                _logger.Information(
                    $"Cached payment methods list with key: {cacheKey}, TTL: {CacheConfig.PaymentMethodsCacheTTL}");
            }
            catch (RedisException redisEx)
            {
                _logger.Warning("Failed to cache payment methods list: {Error}", redisEx.Message);
            }

            return new ApiResponse()
            {
                Status = StatusCodes.Status200OK,
                Message = "Lấy danh sách phương thức thanh toán thành công!",
                Data = paymentMethods
            };
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status401Unauthorized,
            Message = "Bạn không có quyền này!"
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: paymentMethods:list:{name}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetPaymentMethodsQuery request, ERole? role = null,string? brandId = null)
    {
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name;
        var sortBy = request.SortBy ?? "CreatedDate";

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(PaymentMethods)}:{role}:{brandId}")}:{name}:{request.Page}:{request.Size}:{sortBy}:" +
            $"{request.IsAsc}:{request.Code}:{request.Name}:{request.Status}";
    }
}