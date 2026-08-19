using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Menus;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetPublicMenuByBrand;

public class GetPublicMenuByBrandQueryHandler : IRequestHandler<GetPublicMenuByBrandQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IMediaService _mediaService;
    private readonly IRedisService _redisService;

    public GetPublicMenuByBrandQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IMediaService mediaService,
        IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaService = mediaService;
        _redisService = redisService;
    }

    public async ValueTask<ApiResponse> Handle(
        GetPublicMenuByBrandQuery request,
        CancellationToken cancellationToken)
    {
        // 3. Build cache key for first page ONLY
        string cacheKey = BuildCacheKey(request, request.BrandCode);

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");
                var cachedResult = JsonConvert.DeserializeObject<GetPublicBrandMenuResponse>(cachedData,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    });

                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy menu thương hiệu thành công!",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed: {Error}", e.Message);
        }

        // 4. Load ALL categories (NO infinite scroll for categories)
        var allCategories = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .GetListAsync<GetPublicMenuProductCategoryResponse>(
                predicate: x =>
                    x.Brand.Code.Equals(request.BrandCode) && (x.Brand.Status == EBrandStatus.Active) &&
                    x.Status == ECategoryStatus.Active,
                include: x => x.Include(x => x.Brand),
                orderBy: x => x
                    .OrderBy(cat => cat.DisplayOrder)
                    .ThenBy(cat => cat.Name)
            );

        if (!allCategories.Any())
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Thương hiệu này hiện không có sản phẩm nào!"
            };
        }

        // 5. Generate signed URLs for category images
        foreach (var categoryResponse in allCategories)
        {
            if (!string.IsNullOrEmpty(categoryResponse.ImagePath))
            {
                try
                {
                    categoryResponse.ImageUrl = await _mediaService.GetImageUrlAsync(
                        categoryResponse.ImagePath,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for category image {ImageUrl}: {Error}",
                        categoryResponse.ImagePath,
                        ex.Message
                    );
                }
            }
        }

        // 6. Determine target categories
        List<Guid> targetCategoryIds;
        GetPublicMenuProductCategoryResponse? selectedCategory = null;

        if (request.CategoryId.HasValue)
        {
            selectedCategory = allCategories.FirstOrDefault(x => x.Id == request.CategoryId);

            if (selectedCategory == null)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy menu thương hiệu thành công!",
                    Data = new GetPublicBrandMenuResponse
                    {
                        SelectedCategory = null,
                        ProductCategoriesTree = BuildCategoryTree(allCategories, null),
                        Products = new Paginate<GetPublicMenuProductResponse>()
                        {
                            TotalPages = 0,
                            Total = 0,
                            Page = 0,
                            Size = 0,
                            Items = new List<GetPublicMenuProductResponse>()
                        }
                    }
                };
            }

            targetCategoryIds = GetCategoryAndDescendantIds(allCategories, request.CategoryId.Value);
        }
        else
        {
            targetCategoryIds = allCategories.Select(c => c.Id).ToList();
        }

        // 8. Load products with INFINITE SCROLL using composite cursor
        var products = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetPagingListAsync<GetPublicMenuProductResponse>(
                predicate: x => targetCategoryIds.Contains(x.ProductCategoryId) &&
                                x.Status == EProductStatus.Active &&
                                (string.IsNullOrEmpty(request.ProductName) || x.Name.Contains(request.ProductName))
                                && x.ProductSellType == EProductSellType.ProductSell,
                include: i => i.Include(p => p.ProductImages),
                page: request.Page,
                size: request.Size,
                sortBy: request.ProductsSortBy ?? "CreatedDate",
                isAsc: request.ProductsIsAsc
            );

        // 9. Generate signed URLs for product images
        foreach (var product in products.Items)
        {
            if (product.Images != null && product.Images.Any())
            {
                foreach (var image in product.Images)
                {
                    if (!string.IsNullOrEmpty(image.Path))
                    {
                        try
                        {
                            image.Url = await _mediaService.GetImageUrlAsync(
                                image.Path,
                                TimeSpan.FromHours(1)
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(
                                "Failed to generate signed URL for product image {ImageUrl}: {Error}",
                                image.Path,
                                ex.Message
                            );
                        }
                    }
                }

                product.Images = product.Images.OrderByDescending(x => x.IsMainImage).ToList();
            }
        }

        // 10. Build category tree
        var categoryTree = BuildCategoryTree(allCategories, request.CategoryId);

        // 11. Build response
        var response = new GetPublicBrandMenuResponse
        {
            SelectedCategory = selectedCategory != null
                ? MapToCategory(selectedCategory, allCategories, request.CategoryId)
                : null,
            ProductCategoriesTree = categoryTree,
            Products = products
        };

        var apiResponse = new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy menu thương hiệu thành công!",
            Data = response
        };

        // 12. Cache ONLY first page (no cursor)
        if (cacheKey != null)
        {
            try
            {
                var serializedData = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects
                });
                await _redisService.SetStringAsync(
                    cacheKey,
                    serializedData,
                    TimeSpan.FromMinutes(5) // Menu TTL
                );
                _logger.Information($"Cached menu with key: {cacheKey}");
            }
            catch (RedisException redisEx)
            {
                _logger.Warning("Failed to cache menu: {Error}", redisEx.Message);
            }
        }

        return apiResponse;
    }

    #region Helper Methods

    private string BuildCacheKey(GetPublicMenuByBrandQuery request, string brandCode)
    {
        var categoryPart = request.CategoryId.HasValue
            ? request.CategoryId.Value.ToString()
            : "all";

        var productName = string.IsNullOrEmpty(request.ProductName) ? "all" : request.ProductName;
        var sortBy = request.ProductsSortBy ?? "CreatedDate";

        return $"{CacheConfig.EntityListCachePrefix($"public-menu")}:{brandCode}" +
               $":category:{categoryPart}" +
               $":page:{request.Page}" +
               $":size:{request.Size}" +
               $":name:{productName}" +
               $":sort:{sortBy}" +
               $":isAsc:{request.ProductsIsAsc}";
    }

    private List<GetPublicMenuProductCategoryResponse> BuildCategoryTree(
        ICollection<GetPublicMenuProductCategoryResponse> allCategories,
        Guid? selectedCategoryId)
    {
        var rootCategories = allCategories
            .Where(c => c.ParentProductCategoryId == null || c.ParentProductCategoryId == Guid.Empty)
            .ToList();

        return rootCategories
            .Select(c => MapToCategory(c, allCategories, selectedCategoryId))
            .ToList();
    }

    private GetPublicMenuProductCategoryResponse MapToCategory(
        GetPublicMenuProductCategoryResponse category,
        ICollection<GetPublicMenuProductCategoryResponse> allCategories,
        Guid? selectedCategoryId)
    {
        var children = allCategories
            .Where(c => c.ParentProductCategoryId == category.Id)
            .Select(c => MapToCategory(c, allCategories, selectedCategoryId))
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        // Note: ProductCount và TotalProductCount sẽ không chính xác 100%
        // vì products được load riêng với infinite scroll
        // Có thể tính toán nếu cần, nhưng sẽ cost performance
        var totalProductCount = children.Sum(c => c.TotalProductCount);

        category.Children = children;
        category.IsSelected = category.Id == selectedCategoryId;
        category.TotalProductCount = totalProductCount;

        return category;
    }

    private List<Guid> GetCategoryAndDescendantIds(
        ICollection<GetPublicMenuProductCategoryResponse> allCategories,
        Guid categoryId)
    {
        var ids = new List<Guid> { categoryId };

        var children = allCategories
            .Where(c => c.ParentProductCategoryId == categoryId)
            .ToList();

        foreach (var child in children)
        {
            ids.AddRange(GetCategoryAndDescendantIds(allCategories, child.Id));
        }

        return ids;
    }

    #endregion
}

public class GetPublicBrandMenuResponse
{
    public GetPublicMenuProductCategoryResponse SelectedCategory { get; set; }
    public ICollection<GetPublicMenuProductCategoryResponse> ProductCategoriesTree { get; set; }
    public IPaginate<GetPublicMenuProductResponse> Products { get; set; }
}