using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Products;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IMediaService _mediaService;
    private readonly IClaimService _claimService;

    public GetProductsQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IRedisService redisService,
        IMediaService mediaService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _mediaService = mediaService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
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

        var cacheKey = BuildCacheKey(request, role,brandId.ToString());

        #region Thử lấy từ redis

        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetProductsResponse>>(cachedData);

                // QUAN TRỌNG: Regenerate signed URLs vì chúng có expiration
                if (cachedResult?.Items != null && cachedResult.Items.Any())
                {
                    await RegenerateSignedUrlsForProducts(cachedResult.Items, cancellationToken);
                }

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách sản phẩm thành công (from cache)",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        #endregion

        #region Query từ database

        var products = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .GetPagingListAsync<GetProductsResponse>(
                predicate: x => (string.IsNullOrEmpty(request.Name) || x.Name.Contains(request.Name))
                                && (request.Status == null || x.Status == request.Status)
                                && (x.ProductCategory.BrandId == brandId),
                include: x => x.Include(p => p.ProductImages), // Include ProductImages
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );

        #endregion

        foreach (var product in products.Items)
        {
            var mainImagePath = product.MainImagePath;
            string mainImageUrl = string.Empty;

            // Nếu có ảnh, generate signed URL
            if (mainImagePath != null && !string.IsNullOrEmpty(mainImagePath))
            {
                try
                {
                    product.MainImageUrl = await _mediaService.GetImageUrlAsync(
                        mainImagePath,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        mainImagePath,
                        ex.Message
                    );
                }
            }
        }

        #region Cache result

        try
        {
            // Cache toàn bộ pagination result (bao gồm Items đã có signed URL)
            var serializedData = JsonSerializer.Serialize(products);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.ProductsCacheTTL
            );
            _logger.Information(
                "Cached products list with key: {CacheKey}, TTL: {TTL}",
                cacheKey,
                CacheConfig.ProductsCacheTTL
            );
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache products list: {Error}", redisEx.Message);
        }

        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách sản phẩm thành công",
            Data = products
        };
    }

    /// <summary>
    /// Regenerate signed URLs cho cached products
    /// Vì signed URL có expiration time nên cần generate lại khi lấy từ cache
    /// </summary>
    private async Task RegenerateSignedUrlsForProducts(
        IEnumerable<GetProductsResponse> products,
        CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            // Chỉ regenerate nếu có MainImageUrl
            if (!string.IsNullOrEmpty(product.MainImagePath))
            {
                try
                {
                    // Extract filename từ signed URL (nếu cần)
                    // Hoặc lưu filename riêng trong cache
                    var fileName = ExtractFileNameFromUrl(product.MainImagePath);

                    product.MainImageUrl = await _mediaService.GetImageUrlAsync(
                        fileName,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to regenerate signed URL for product {ProductId}: {Error}",
                        product.Id,
                        ex.Message
                    );
                    // Giữ nguyên URL cũ nếu không regenerate được
                }
            }
        }
    }

    /// <summary>
    /// Extract filename từ URL hoặc signed URL
    /// VD: "products/abc-123.jpg" từ signed URL
    /// </summary>
    private string ExtractFileNameFromUrl(string url)
    {
        // Nếu là signed URL, extract phần object name
        // VD: https://storage.googleapis.com/bucket/products%2Fabc-123.jpg?GoogleAccessId=...

        if (url.Contains("storage.googleapis.com"))
        {
            var uri = new Uri(url);
            var pathSegments = uri.AbsolutePath.Split('/');
            var fileName = Uri.UnescapeDataString(pathSegments[^1]); // Decode URL encoding
            return fileName;
        }

        // Nếu là plain filename
        return url;
    }

    /// <summary>
    /// Tạo cache key duy nhất cho mỗi query
    /// Format: products:list:{name}:{status}:{page}:{size}:{sortBy}:{isAsc}
    /// </summary>
    private string BuildCacheKey(GetProductsQuery request, ERole role,string brandId)
    {
        var name = string.IsNullOrEmpty(request.Name) ? "all" : request.Name;
        var status = request.Status?.ToString() ?? "all";
        var sortBy = request.SortBy ?? "CreatedDate";

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Products)}:{role}:{brandId}")}:{name}:{status}:{request.Page}:{request.Size}:{sortBy}:{request.IsAsc}";
    }
}