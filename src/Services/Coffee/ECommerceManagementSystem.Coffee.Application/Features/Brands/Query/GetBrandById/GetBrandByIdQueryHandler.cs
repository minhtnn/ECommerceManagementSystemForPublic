using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Brands;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrandById;

public class GetBrandByIdQueryHandler : IRequestHandler<GetBrandByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;


    public GetBrandByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        ICacheInvalidationService cacheService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
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
            var cachedBrand = await _cacheService.GetDetailFromCacheAsync<GetBrandByIdResponse>(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Brands), request.BrandId.ToString())
            );

            if (cachedBrand != null)
            {
                _logger.Debug($"Cache HIT for brand:{request.BrandId}");
                cachedBrand.CreatedDate = TimeUtil.ConvertFromUtc(cachedBrand.CreatedDate, request.TimeZone);
                cachedBrand.LastModifiedDate = TimeUtil.ConvertFromUtc(cachedBrand.LastModifiedDate, request.TimeZone);
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin thương hiệu thành công",
                    Data = cachedBrand
                };
            }

            _logger.Debug($"Cache MISS for brand:{request.BrandId}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var brand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync<GetBrandByIdResponse>(
                predicate: x => x.Id == request.BrandId
            );
        if (brand == null)
        {
            throw new BadHttpRequestException("Không tìm thấy thương hiệu với ID đã cho");
        }

        if (brand.LogoPath != null && !string.IsNullOrEmpty(brand.LogoPath))
        {
            try
            {
                brand.LogoUrl = await _mediaService.GetImageUrlAsync(
                    brand.LogoPath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Failed to generate signed URL for image {ImageUrl}: {Error}",
                    brand.LogoPath,
                    ex.Message
                );
            }
        }

        brand.CreatedDate = TimeUtil.ConvertFromUtc(brand.CreatedDate, request.TimeZone);
        brand.LastModifiedDate = TimeUtil.ConvertFromUtc(brand.LastModifiedDate, request.TimeZone);
        try
        {
            await _cacheService.SetDetailToCacheAsync(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Brands), request.BrandId.ToString()), brand,
                CacheConfig.BrandsCacheTTL
            );
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache brand {BrandId}: {Error}", brand.Id, e.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin thương hiệu thành công",
            Data = brand
        };
    }
}