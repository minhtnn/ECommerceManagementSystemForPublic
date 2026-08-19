using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategoryById;

public class GetProductCategoryByIdQueryHandler : IRequestHandler<GetProductCategoryByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetProductCategoryByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetProductCategoryByIdQuery request, CancellationToken cancellationToken)
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

        try
        {
            var cachedProductCategory = await _cacheService.GetDetailFromCacheAsync<GetProductCategoryByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.ProductCategories), request.Id.ToString())}:{role}:{brandId.ToString()}"
            );
            if (cachedProductCategory != null)
            {
                _logger.Debug($"Cache HIT for product category:{request.Id}");
                cachedProductCategory.CreatedDate = TimeUtil.ConvertFromUtc(cachedProductCategory.CreatedDate, request.TimeZone);
                cachedProductCategory.LastModifiedDate = TimeUtil.ConvertFromUtc(cachedProductCategory.LastModifiedDate, request.TimeZone);
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách danh mục thành công",
                    Data = cachedProductCategory
                };
            }

            _logger.Debug($"Cache MISS for product category:{request.Id}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var category = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .SingleOrDefaultAsync<GetProductCategoryByIdResponse>(
                predicate: (x => x.Id == request.Id && x.BrandId == brandId)
            );
        if (category == null)
        {
            throw new BadHttpRequestException("Không tìm thấy danh mục sản phẩm với ID đã cho");
        }

        if (category.ImagePath != null && !string.IsNullOrEmpty(category.ImagePath))
        {
            try
            {
                category.ImageUrl = await _mediaService.GetImageUrlAsync(
                    category.ImagePath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Failed to generate signed URL for image {ImageUrl}: {Error}",
                    category.ImagePath,
                    ex.Message
                );
            }
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.ProductCategories), request.Id.ToString())}:{role}:{brandId.ToString()}",
                category, CacheConfig.ProductCategoriesCacheTTL);
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache product category {ProductId}: {Error}", category.Id, e.Message);
        }

        if (category != null)
        {
            category.CreatedDate = TimeUtil.ConvertFromUtc(category.CreatedDate, request.TimeZone);
            category.LastModifiedDate = TimeUtil.ConvertFromUtc(category.LastModifiedDate, request.TimeZone);
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin danh mục sản phẩm thành công",
            Data = category
        };
    }
}