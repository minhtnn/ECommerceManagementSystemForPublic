using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Products;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProductById;

public class GetPublicProductByIdQueryHandler :
    IRequestHandler<GetPublicProductByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheService;
    private readonly ILogger _logger;
    private readonly IMediaService _mediaService;

    public GetPublicProductByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IMediaService mediaService, ICacheInvalidationService cacheService) : base()
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaService = mediaService;
        _cacheService = cacheService;
    }


    public async ValueTask<ApiResponse> Handle(GetPublicProductByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cachedProducts = await _cacheService.GetDetailFromCacheAsync<GetProductByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Products), request.ProductId.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public"
            );

            if (cachedProducts != null)
            {
                _logger.Debug($"Cache HIT for product:{request.ProductId}");
                cachedProducts.CreatedDate = TimeUtil.ConvertFromUtc(cachedProducts.CreatedDate, request.TimeZone);
                cachedProducts.LastModifiedDate = cachedProducts.LastModifiedDate.HasValue
                    ? TimeUtil.ConvertFromUtc(cachedProducts.LastModifiedDate.Value, request.TimeZone)
                    : null;
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy thông tin sản phẩm thành công",
                    Data = cachedProducts
                };
            }

            _logger.Debug($"Cache MISS for product:{request.ProductId}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var product = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .SingleOrDefaultAsync<GetProductByIdResponse>(
                predicate: x => (x.Id == request.ProductId)
                                && (x.ProductCategory.Brand.Code == request.BrandCode)
                                && (x.Status == EProductStatus.Active),
                include: x => x.Include(x => x.ProductCategory)
                    .ThenInclude(x => x.Brand)
            );
        if (product == null)
        {
            throw new BadHttpRequestException("Không tìm thấy sản phẩm với ID đã cho");
        }

        if (product.GetProductImagesResponse != null && product.GetProductImagesResponse.Any())
        {
            foreach (var image in product.GetProductImagesResponse)
            {
                try
                {
                    image.ImageUrl = await _mediaService.GetImageUrlAsync(
                        image.ImagePath,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        image.ImagePath,
                        ex.Message
                    );
                }
            }

            product.GetProductImagesResponse = product.GetProductImagesResponse
                .OrderByDescending(img => img.IsMainImage).ToList();
        }

        if (product.GetProductSideAttributesResponse != null && product.GetProductSideAttributesResponse.Any())
        {
            product.GetProductSideAttributesResponse = product.GetProductSideAttributesResponse
                .Select(attr => new GetProductByIdSideAttributesResponse
                {
                    Id = attr.Id,
                    Key = attr.Key,
                    Value = attr.Value
                })
                .ToList();
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Products), request.ProductId.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public",
                product, CacheConfig.ProductsCacheTTL);
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache product {ProductId}: {Error}", product.Id, e.Message);
        }

        if (product != null)
        {
            product.CreatedDate = TimeUtil.ConvertFromUtc(product.CreatedDate, request.TimeZone);
            product.LastModifiedDate = product.LastModifiedDate.HasValue
                ? TimeUtil.ConvertFromUtc(product.LastModifiedDate.Value, request.TimeZone)
                : null;
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin sản phẩm thành công",
            Data = product
        };
    }
}