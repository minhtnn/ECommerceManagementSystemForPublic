using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategories;

public class GetProductCategoriesQueryHandler : IRequestHandler<GetProductCategoriesQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetProductCategoriesQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
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

        var cacheKey = BuildCacheKey(request, role, brandId.ToString());

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetProductCategoriesResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách danh mục sản phẩm thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var categories = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .GetPagingListAsync<GetProductCategoriesResponse>(
                predicate: x => ((string.IsNullOrEmpty(request.Code) || x.Code.Contains(request.Code))
                                  && (string.IsNullOrEmpty(request.Name) || x.Name.Contains(request.Name))
                                  && (request.Status == null || x.Status == request.Status)
                                  && (request.IsLeafOnly == null || x.IsLeafOnly == request.IsLeafOnly )
                                  && (x.BrandId == brandId)),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );

        #region Assign image

        foreach (var category in categories.Items)
        {
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
        }

        #endregion

        try
        {
            var serializedData = JsonSerializer.Serialize(categories);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.ProductCategoriesCacheTTL
            );
            _logger.Information(
                $"Cached product categories list with key: {cacheKey}, TTL: {CacheConfig.ProductCategoriesCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache product categories list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách danh mục sản phẩm thành công",
            Data = categories
        };
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: products:list:{name}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetProductCategoriesQuery request, ERole role,string brandId)
    {
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name;
        var code = string.IsNullOrEmpty(request.Code) ? "all" : request.Code;
        var status = (request.Status == null) ? "all" : request.Status.ToString();
        var isLeafOnly = request.IsLeafOnly ?? null;
        var sortBy = request.SortBy ?? "CreatedDate";
        

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(ProductCategories)}:{role}:{brandId}")}:{code}:{name}:{isLeafOnly}:{status}:{request.Page}" +
            $":{request.Size}:{sortBy}:{request.IsAsc}";
    }
}