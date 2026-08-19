using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Posts;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPostById;

public class GetBrandPostByIdQueryHandler : IRequestHandler<GetBrandPostByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public GetBrandPostByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        ICacheInvalidationService cacheService, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPostByIdQuery request, CancellationToken cancellationToken)
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
            var cachedPost = await _cacheService.GetDetailFromCacheAsync<GetBrandPostByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{role}:{brandId.ToString()}"
            );
            if (cachedPost != null)
            {
                _logger.Debug($"Cache HIT for post:{request.Id}");
                cachedPost.CreatedDate = TimeUtil.ConvertFromUtc(cachedPost.CreatedDate, request.TimeZone);
                cachedPost.LastModifiedDate = TimeUtil.ConvertFromUtc(cachedPost.LastModifiedDate, request.TimeZone);
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách bài đăng thành công",
                    Data = cachedPost
                };
            }

            _logger.Debug($"Cache MISS for post:{request.Id}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var post = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .SingleOrDefaultAsync<GetBrandPostByIdResponse>(
                predicate: (x => x.Id == request.Id && x.BrandId == brandId)
            );
        if (post == null)
        {
            throw new BadHttpRequestException("Không tìm thấy bài đăng với ID đã cho");
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), request.Id.ToString())}:{role}:{brandId.ToString()}",
                post, CacheConfig.PostsCacheTTL);
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache post {PostId}: {Error}", post.Id, e.Message);
        }

        if (!string.IsNullOrWhiteSpace(post.ImagePath))
        {
            try
            {
                post.ImageUrl = await _mediaService.GetImageUrlAsync(
                    post.ImagePath,
                    TimeSpan.FromHours(1)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to sign featured image {P}: {E}",
                    post.ImagePath, ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            try
            {
                post.Content = await _mediaService.ResolveContentImageUrlsAsync(
                    post.Content,
                    TimeSpan.FromDays(7)
                );
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to resolve inline images for post {Id}: {E}",
                    post.Id, ex.Message);
            }
        }

        if (post != null)
        {
            post.CreatedDate = TimeUtil.ConvertFromUtc(post.CreatedDate, request.TimeZone);
            post.LastModifiedDate = TimeUtil.ConvertFromUtc(post.LastModifiedDate, request.TimeZone);
        }
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin bài đăng thành công",
            Data = post
        };
    }
}