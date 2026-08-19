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

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;

public class CreateBrandPostCommandHandler : IRequestHandler<CreateBrandPostCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public CreateBrandPostCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
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

    // Features/Posts/Command/CreateBrandPost/CreateBrandPostCommandHandler.cs
    public async ValueTask<ApiResponse> Handle(
        CreateBrandPostCommand request,
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

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        var existedPost = await _unitOfWork.GetRepository<Domain.Entities.Posts>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code == request.Code && x.BrandId == brandId
            );
        if (existedPost != null)
            throw new BadHttpRequestException("Mã bài đăng đã tồn tại");

        var brand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == brandId
            );
        if (brand == null)
            throw new BadHttpRequestException("Thương hiệu không tồn tại");

        var post = _mapper.Map<Domain.Entities.Posts>(request);
        post.Id = Guid.CreateVersion7();
        post.BrandId = brandId;
        post.Author = brand.Name;
        post.CreatedDate = DateTime.UtcNow;
        post.LastModifiedDate = DateTime.UtcNow;
        post.Status = EPostStatus.PendingReview;

        // Track để rollback nếu lỗi
        var uploadedFeaturedImage = string.Empty;
        var uploadedInlineImages = new List<string>();

        try
        {
            // 1. Upload featured image
            if (request.Image != null)
            {
                if (!ImageUtil.IsValidImageFile(request.Image))
                    throw new BadHttpRequestException(
                        "Featured image không hợp lệ. " +
                        "Chỉ chấp nhận .jpg, .jpeg, .png, .gif, .webp và <= 5MB"
                    );

                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Image,
                    folderPath: nameof(Domain.Entities.Posts).ToLowerInvariant(),
                    cancellationToken
                );

                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                    throw new Exception($"Không thể upload featured image: {uploadResult.Message}");

                uploadedFeaturedImage = uploadResult.FileName;
                post.FeaturedImage = uploadResult.FileName;
            }

            var normalizedContent = await _mediaService.NormalizeContentImageUrlsAsync(
                request.Content
            );

            // 3. Upload inline images + resolve placeholder
            var (resolvedContent, inlineFileNames) =
                await PostContentUtil.UploadInlineImagesAsync(
                    normalizedContent,
                    request.InlineImages,
                    _mediaService,
                    brandId,
                    cancellationToken
                );
            uploadedInlineImages = inlineFileNames;
            post.Content = resolvedContent;

            // 3. Insert + Commit
            await _unitOfWork.GetRepository<Domain.Entities.Posts>().InsertAsync(post);

            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
                throw new Exception($"Không thể tạo bài đăng: {commitResult.Message}");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to create post for brand {BrandId}", brandId);
            await _unitOfWork.RollbackTransactionAsync();

            // Rollback ảnh đã upload
            if (!string.IsNullOrEmpty(uploadedFeaturedImage))
                await _mediaService.DeleteFileAsync(uploadedFeaturedImage, cancellationToken);

            await PostContentUtil.RollbackInlineImagesAsync(
                uploadedInlineImages, _mediaService, _logger, cancellationToken
            );

            throw;
        }

        // Cache invalidation (giữ nguyên như cũ)
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.Posts)}:{role}:{brandId}"
                )
            ),
            operation: EOperationBeforeCache.BulkCreate,
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

        if (cacheResult.Success)
            _logger.Information(
                "Created post '{Code}' (ID: {Id}). Cache: {CacheMessage}",
                post.Code, post.Id, cacheResult.Message
            );
        else
            _logger.Warning(
                "Created post '{Code}' but cache invalidation failed: {CacheMessage}",
                post.Code, cacheResult.Message
            );

        return new ApiResponse
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo mới bài đăng thành công",
            Data = post.Id,
        };
    }
}