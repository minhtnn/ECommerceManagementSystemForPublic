using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Posts;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostOgOverviewById;

public class
    GetBrandPublicPostOgOverviewByIdQueryHandler : IRequestHandler<GetBrandPublicPostOgOverviewByIdQuery, IActionResult>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IMediaService _mediaService;

    public GetBrandPublicPostOgOverviewByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _mediaService = mediaService;
    }

    public async ValueTask<IActionResult> Handle(GetBrandPublicPostOgOverviewByIdQuery request,
        CancellationToken cancellationToken)
    {
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.BrandCode)
            );

        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại!");
        }

        var brandSetting = SettingUtil.Parse<BrandSetting>(existedBrand.Configuration);

        if (!brandSetting.EnableOgPostFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng open graph!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl) ||
            string.IsNullOrWhiteSpace(brandSetting.FrontEndPostPath))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin open graph!");
        }

        GetPublicBrandPostByIdResponse? post = null;

        try
        {
            var cachedPost = await _cacheService.GetDetailFromCacheAsync<GetPublicBrandPostByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public"
            );

            if (cachedPost != null)
            {
                _logger.Debug($"Cache HIT for post:{request.Id}");
                post = cachedPost;
            }
            else
            {
                _logger.Debug($"Cache MISS for post:{request.Id}");
            }
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        if (post == null)
        {
            post = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
                .SingleOrDefaultAsync<GetPublicBrandPostByIdResponse>(
                    predicate: x => x.Id == request.Id
                                    && x.Brand.Code == request.BrandCode
                                    && x.Brand.Status == EBrandStatus.Active
                                    && x.Status == EPostStatus.Published,
                    include: x => x.Include(x => x.Brand)
                );

            if (post == null)
                return new NotFoundResult();

            if (!string.IsNullOrWhiteSpace(post.ImagePath))
            {
                try
                {
                    // Dùng TTL dài cho OG image vì bot cache ảnh lâu
                    post.ImageUrl = await _mediaService.GetImagePermanentUrlAsync(
                        post.ImagePath
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning("Failed to sign featured image {P}: {E}", post.ImagePath, ex.Message);
                }
            }

            try
            {
                await _cacheService.SetDetailToCacheAsync(
                    $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{ERole.EndCustomer}:{request.BrandCode}:public",
                    post, CacheConfig.PostsCacheTTL);
            }
            catch (RedisException e)
            {
                _logger.Warning("Failed to cache post {Id}: {Error}", post.Id, e.Message);
            }
        }

        return BuildPostOgPreview(
            brandSiteName: existedBrand.Name,
            brandFrontendUrl: brandSetting.FrontEndUrl, 
            postPath: brandSetting.FrontEndPostPath,
            post: post);
    }

    private static ContentResult BuildPostOgPreview(string brandSiteName, string brandFrontendUrl, string postPath,
        GetPublicBrandPostByIdResponse post)
    {
        var frontendUrl = $"https://{brandFrontendUrl}/{postPath}/{post.Id}";
        var imageUrl = post.ImageUrl;
        var title = post.Title;
        var description = post.Excerpt;
        var author = post.Author?.Trim();

        var html = $"""
                    <!DOCTYPE html>
                    <html lang="vi">
                    <head>
                        <meta charset="UTF-8"/>
                        <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                        <title>{title}</title>

                        <meta name="description"  content="{description}"/>
                        <meta name="author"       content="{author}"/>
                        <meta name="robots"       content="index, follow"/>
                        <link rel="canonical"     href="{frontendUrl}"/>

                        <meta property="og:title"            content="{title}"/>
                        <meta property="og:description"      content="{description}"/>
                        <meta property="og:type"             content="article"/>
                        <meta property="og:url"              content="{frontendUrl}"/>
                        <meta property="og:image"            content="{imageUrl}"/>
                        <meta property="og:image:secure_url" content="{imageUrl}"/>
                        <meta property="og:image:type"       content="image/png"/>
                        <meta property="og:image:width"      content="1200"/>
                        <meta property="og:image:height"     content="630"/>
                        <meta property="og:site_name"        content="{brandSiteName}"/>
                        <meta property="og:locale"           content="vi_VN"/>

                        <meta name="twitter:card"            content="summary_large_image"/>
                        <meta name="twitter:title"           content="{title}"/>
                        <meta name="twitter:description"     content="{description}"/>
                        <meta name="twitter:image"           content="{imageUrl}"/>
                        <meta name="twitter:image:alt"       content="{title}"/>
                    </head>
                    <body>
                        <p>Đang chuyển hướng... <a href="{frontendUrl}">Nhấn vào đây nếu không tự chuyển</a></p>
                        <script>window.location.replace("{frontendUrl}")</script>
                    </body>
                    </html>
                    """;

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}