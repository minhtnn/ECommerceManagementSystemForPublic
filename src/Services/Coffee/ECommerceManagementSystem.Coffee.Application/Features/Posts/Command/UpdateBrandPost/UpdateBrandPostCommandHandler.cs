using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.UpdateBrandPost;

public class UpdateBrandPostCommandHandler : IRequestHandler<UpdateBrandPostCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public UpdateBrandPostCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, ILogger logger, IMapper mapper, IClaimService claimService,
        IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    // Features/Posts/Command/UpdateBrandPost/UpdateBrandPostCommandHandler.cs
    public async ValueTask<ApiResponse> Handle(
        UpdateBrandPostCommand request,
        CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var existingPost = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.Id && x.BrandId == brandId
            );
        if (existingPost == null)
            throw new BadHttpRequestException("Bài đăng không tồn tại");

        // Lưu lại trước khi update để xóa sau
        var oldFeaturedImage = existingPost.FeaturedImage ?? string.Empty;
        var oldInlineImagePaths = PostContentUtil
            .ExtractInlineImagePaths(existingPost.Content);

        // Normalize signed URLs trong content FE gửi lên
        var normalizedContent = await _mediaService.NormalizeContentImageUrlsAsync(
            request.Content
        );

        // Kiểm tra content thay đổi (so với normalized)
        bool hasContentChanged =
            existingPost.Title != request.Title
            || existingPost.Slug != request.Slug
            || existingPost.Content != normalizedContent
            || existingPost.Excerpt != request.Excerpt
            || request.Image != null
            || (request.InlineImages != null && request.InlineImages.Count > 0);

        var targetStatus = hasContentChanged
            ? EPostStatus.PendingReview
            : request.Status;

        // Validate transition
        if (existingPost.Status != targetStatus)
        {
            bool isValid = existingPost.Status switch
            {
                EPostStatus.PendingReview => targetStatus is
                    EPostStatus.NeedsRevision or
                    EPostStatus.Published or
                    EPostStatus.Hidden,
                EPostStatus.NeedsRevision => targetStatus is
                    EPostStatus.PendingReview or
                    EPostStatus.Published or
                    EPostStatus.Hidden,
                EPostStatus.Published => targetStatus is
                    EPostStatus.Hidden,
                EPostStatus.Hidden => targetStatus == EPostStatus.Published,
                _ => false
            };

            if (!isValid)
                throw new BadHttpRequestException(
                    $"Không thể chuyển từ '{existingPost.Status.ToString()}' sang '{targetStatus}'"
                );
        }

        var isStatusChanged = existingPost.Status != targetStatus;
        var uploadedFeaturedImage = string.Empty;
        var uploadedInlineImages = new List<string>();

        try
        {
            // 1. Upload featured image mới
            if (request.Image != null)
            {
                if (!ImageUtil.IsValidImageFile(request.Image))
                    throw new BadHttpRequestException("Featured image không hợp lệ.");

                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Image,
                    folderPath: nameof(Domain.Entities.Posts).ToLowerInvariant(),
                    cancellationToken
                );
                if (!uploadResult.IsSuccess)
                    throw new Exception(
                        $"Không thể upload featured image: {uploadResult.Message}"
                    );

                uploadedFeaturedImage = uploadResult.FileName;
                existingPost.FeaturedImage = uploadResult.FileName;
            }

            // 2. Upload inline images mới + resolve placeholder
            var (resolvedContent, inlineFileNames) =
                await PostContentUtil.UploadInlineImagesAsync(
                    normalizedContent,
                    request.InlineImages,
                    _mediaService,
                    brandId,
                    cancellationToken
                );
            uploadedInlineImages = inlineFileNames;

            // 3. Update fields
            existingPost.Title = request.Title;
            existingPost.Slug = request.Slug;
            existingPost.Content = resolvedContent;
            existingPost.Excerpt = request.Excerpt;
            existingPost.Status = targetStatus;
            existingPost.LastModifiedDate = DateTime.UtcNow;

            if (targetStatus == EPostStatus.Published
                && existingPost.PublishedAt == null)
                existingPost.PublishedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<Domain.Entities.Posts>()
                .UpdateAsync(existingPost);

            var isSuccess = _unitOfWork.Commit() > 0;
            if (!isSuccess)
                throw new Exception("Cập nhật bài đăng không thành công!");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to update post {PostId}", request.Id);

            if (!string.IsNullOrEmpty(uploadedFeaturedImage))
                await _mediaService.DeleteFileAsync(
                    uploadedFeaturedImage, cancellationToken
                );

            await PostContentUtil.RollbackInlineImagesAsync(
                uploadedInlineImages, _mediaService, _logger, cancellationToken
            );
            throw;
        }

        // Xóa ảnh cũ sau commit thành công
        if (!string.IsNullOrEmpty(uploadedFeaturedImage)
            && !string.IsNullOrEmpty(oldFeaturedImage))
        {
            try
            {
                await _mediaService.DeleteFileAsync(oldFeaturedImage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to delete old featured image {F}", oldFeaturedImage);
            }
        }

        var newInlinePaths = PostContentUtil
            .ExtractInlineImagePaths(existingPost.Content);
        var removedInlinePaths = oldInlineImagePaths.Except(newInlinePaths).ToList();

        if (removedInlinePaths.Count > 0)
            await PostContentUtil.RollbackInlineImagesAsync(
                removedInlinePaths, _mediaService, _logger, cancellationToken
            );
        // ── Cache invalidation (giữ nguyên như cũ) ──
        var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.Posts)}:{role}:{brandId}"
                )
            ),
            operation: isStatusChanged
                ? EOperationBeforeCache.BulkUpdate
                : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.Posts)}:{role}:{brandId}"
                )
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix(
                $"{nameof(Domain.Entities.Posts)}:{role}:{brandId}"
            )
        );

        var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), existingPost.Id.ToString())}:{role}:{brandId}"
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), existingPost.Id.ToString())}:{role}:{brandId}"
            ),
            entityCachePrefix:
            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Posts), existingPost.Id.ToString())}:{role}:{brandId}"
        );

        if (cacheListResult.Success && cacheByIdResult.Success)
            _logger.Information(
                "Updated post (ID: {Id}). Cache: {CacheListMessage}, {CacheDetailMessage}",
                existingPost.Id, cacheListResult.Message, cacheByIdResult.Message
            );
        else
            _logger.Warning(
                "Updated post '{Id}' but cache invalidation failed: {CacheListMessage}, {CacheDetailMessage}",
                existingPost.Id, cacheListResult.Message, cacheByIdResult.Message
            );

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Cập nhật bài đăng thành công",
            Data = existingPost.Id,
        };
    }
}