using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Command.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IMediaService _mediaService;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;

    public UpdateProductCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation,
        ILogger logger,
        IMapper mapper,
        IMediaService mediaService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _mediaService = mediaService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
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
        
        var uploadedFileNames = new List<string>();
        var deletedFileNames = new List<string>();
        var imagesToDeleteFromDb = new List<ProductImages>();

        #region Get existing product with related data

        var existingProduct = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.Id,
                include: q => q.Include(p => p.ProductCategory)
                    .ThenInclude(c => c.Brand)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductSideAttributes)
            );

        if (existingProduct == null)
        {
            throw new BadHttpRequestException("Sản phẩm không tồn tại");
        }

        #endregion

        #region Validate business rules

        if (existingProduct.Status == EProductStatus.Active &&
            request.Status == EProductStatus.Discontinued &&
            request.StockQuantity > 0)
        {
            throw new BadHttpRequestException(
                "Không thể ngừng kinh doanh sản phẩm còn tồn kho. Vui lòng xử lý hết tồn kho trước."
            );
        }

        if (request.Status == EProductStatus.Active && request.StockQuantity == 0)
        {
            _logger.Warning(
                "Product {ProductId} is being set to Active with 0 stock quantity",
                request.Id
            );
            throw new BadHttpRequestException(
                "Sản phẩm đã hết! Hãy cập nhật lại số lượng hàng tồn!"
            );
        }

        if (request.Status == EProductStatus.Active &&
            existingProduct.ProductCategory?.Status != ECategoryStatus.Active)
        {
            throw new BadHttpRequestException(
                $"Không thể kích hoạt sản phẩm khi danh mục '{existingProduct.ProductCategory?.Name}' không hoạt động"
            );
        }

        #endregion

        var isStatusChanged = (existingProduct.Status != request.Status);

        #region Update basic product information

        existingProduct.Name = request.Name;
        existingProduct.FullName = string.IsNullOrWhiteSpace(request.FullName)
            ? request.Name
            : request.FullName;
        existingProduct.Description = request.Description;
        existingProduct.Price = request.Price;
        existingProduct.DisplayOrder = request.DisplayOrder ?? 1;
        existingProduct.Status = request.Status;
        existingProduct.ProductSellType = request.ProductSellType;
        existingProduct.StockQuantity = request.StockQuantity;
        existingProduct.LastModifiedDate = DateTime.UtcNow;

        #endregion
        
        #region Update existing image metadata

        if (request.ExistingImageMetadata != null && request.ExistingImageMetadata.Any())
        {
            var imageRepo = _unitOfWork.GetRepository<ProductImages>();
    
            foreach (var meta in request.ExistingImageMetadata)
            {
                var existingImg = existingProduct.ProductImages.FirstOrDefault(img => img.Id == meta.Id);
                if (existingImg == null) continue;

                existingImg.AltText = meta.AltText;
                existingImg.IsMainImage = meta.IsMainImage;
                existingImg.LastModifiedDate = DateTime.UtcNow;
        
                imageRepo.UpdateAsync(existingImg);
            }
    
            _logger.Information("Updated metadata for {Count} existing images", request.ExistingImageMetadata.Count);
        }

        #endregion

        #region Handle Product Images - PHASE 1: Prepare deletions

        try
        {
            var existingImageIds = request.ExistingImageIds ?? new List<Guid>();
            imagesToDeleteFromDb = existingProduct.ProductImages
                .Where(img => !existingImageIds.Contains(img.Id))
                .ToList();

            // Validate total images after changes
            var remainingImagesCount = existingProduct.ProductImages.Count - imagesToDeleteFromDb.Count;
            var newImagesCount = request.NewImageFiles?.Count ?? 0;
            var totalImagesAfterUpdate = remainingImagesCount + newImagesCount;

            if (totalImagesAfterUpdate > 4)
            {
                throw new BadHttpRequestException(
                    $"Tổng số ảnh không được vượt quá 4. " +
                    $"Hiện tại: {existingProduct.ProductImages.Count}, " +
                    $"Giữ lại: {remainingImagesCount}, " +
                    $"Thêm mới: {newImagesCount}"
                );
            }

            _logger.Information(
                "Image update plan: Current={Current}, Keep={Keep}, Delete={Delete}, Add={Add}",
                existingProduct.ProductImages.Count,
                remainingImagesCount,
                imagesToDeleteFromDb.Count,
                newImagesCount
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating image updates");
            throw;
        }

        #endregion

        #region Handle Product Images - PHASE 2: Upload new images (BEFORE transaction)

        var newProductImages = new List<ProductImages>();

        if (request.NewImageFiles != null && request.NewImageFiles.Any())
        {
            _logger.Information("Starting upload {Count} new images to Firebase", request.NewImageFiles.Count);

            var metadata = request.UpdateNewImageMetadata ?? new List<UpdateProductImageMetadata>();

            for (int i = 0; i < request.NewImageFiles.Count; i++)
            {
                var file = request.NewImageFiles[i];

                try
                {
                    // Validate file
                    if (!ImageUtil.IsValidImageFile(file))
                    {
                        throw new BadHttpRequestException(
                            $"File ảnh thứ {i + 1} không hợp lệ. " +
                            $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                        );
                    }

                    // Upload to Firebase
                    var uploadResult = await _mediaService.UploadImageFromFormAsync(
                        file,
                        folderPath: nameof(Domain.Entities.Products).ToLowerInvariant(),
                        cancellationToken
                    );

                    if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                    {
                        throw new Exception(
                            $"Không thể upload ảnh thứ {i + 1}: {uploadResult.Message}"
                        );
                    }

                    uploadedFileNames.Add(uploadResult.FileName);

                    var imageMeta = metadata.ElementAtOrDefault(i);

                    // Create ProductImage entity (but don't add to context yet)
                    var newProductImage = new ProductImages
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = existingProduct.Id,
                        ImageUrl = uploadResult.FileName,
                        AltText = imageMeta?.AltText,
                        IsMainImage = imageMeta?.IsMainImage ?? false,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    newProductImages.Add(newProductImage);

                    _logger.Information(
                        "Uploaded new image {Index}/{Total}: {FileName}",
                        i + 1,
                        request.NewImageFiles.Count,
                        uploadResult.FileName
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error uploading image {Index}", i + 1);

                    // Rollback uploaded files
                    await CleanupUploadedFiles(uploadedFileNames, cancellationToken);

                    throw;
                }
            }
        }

        #endregion

        #region Start Transaction and Database Operations

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Không thể bắt đầu transaction: {Message}", transactionResult.Message);

            // Cleanup uploaded files
            await CleanupUploadedFiles(uploadedFileNames, cancellationToken);

            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        try
        {
            #region Delete old images from Database

            if (imagesToDeleteFromDb.Any())
            {
                _logger.Information("Deleting {Count} old images from database", imagesToDeleteFromDb.Count);

                var imageRepo = _unitOfWork.GetRepository<ProductImages>();

                foreach (var imageToDelete in imagesToDeleteFromDb)
                {
                    // Track filename for Firebase deletion later
                    deletedFileNames.Add(imageToDelete.ImageUrl);
                    
                    // Delete the image using repository
                    imageRepo.DeleteAsync(imageToDelete);
                    
                    _logger.Information("Marked image for deletion: {ImageUrl}", imageToDelete.ImageUrl);
                }
            }

            #endregion

            #region Add new images to Database

            if (newProductImages.Any())
            {
                var imageRepo = _unitOfWork.GetRepository<ProductImages>();
                
                foreach (var newImage in newProductImages)
                {
                    await imageRepo.InsertAsync(newImage);
                }

                _logger.Information("Added {Count} new images to database", newProductImages.Count);
            }

            #endregion

            #region Ensure at least one main image

            // Get all images that will remain after deletion
            var remainingImages = existingProduct.ProductImages
                .Where(img => !imagesToDeleteFromDb.Any(del => del.Id == img.Id))
                .Concat(newProductImages)
                .ToList();

            if (remainingImages.Any())
            {
                if (!remainingImages.Any(x => x.IsMainImage))
                {
                    remainingImages.First().IsMainImage = true;
                    _logger.Information("Set first image as main image");
                }
            }
            else
            {
                _logger.Warning("Product {ProductId} has no images after update", existingProduct.Id);
            }

            #endregion

            #region Handle Side Attributes

            if (request.SideAttibutes != null)
            {
                var attrRepo = _unitOfWork.GetRepository<ProductSideAttributes>();
                
                // Delete all old attributes
                var oldAttributes = existingProduct.ProductSideAttributes.ToList();
                foreach (var oldAttr in oldAttributes)
                {
                    attrRepo.DeleteAsync(oldAttr);
                }
                
                _logger.Information("Deleted {Count} old side attributes", oldAttributes.Count);

                // Add new attributes
                foreach (var attr in request.SideAttibutes)
                {
                    var newAttribute = new ProductSideAttributes()
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = existingProduct.Id,
                        Key = attr.Key,
                        Value = attr.Value,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    await attrRepo.InsertAsync(newAttribute);
                }

                _logger.Information("Created {Count} new side attributes", request.SideAttibutes.Count);
            }

            #endregion

            #region Commit Database Transaction

            _unitOfWork.GetRepository<Domain.Entities.Products>().UpdateAsync(existingProduct);

            var commitResult = await _unitOfWork.CommitTransactionAsync();

            if (!commitResult.IsSuccess)
            {
                _logger.Error(
                    "Transaction commit failed: {Message}. Exception: {Exception}",
                    commitResult.Message,
                    commitResult.Exception?.Message
                );

                throw new Exception($"Không thể cập nhật sản phẩm: {commitResult.Message}");
            }

            #endregion

            #region Delete old images from Firebase (after DB commit success)

            if (deletedFileNames.Any())
            {
                _logger.Information("Deleting {Count} old images from Firebase", deletedFileNames.Count);

                foreach (var fileName in deletedFileNames)
                {
                    try
                    {
                        var deleteResult = await _mediaService.DeleteFileAsync(fileName, cancellationToken);

                        if (deleteResult)
                        {
                            _logger.Information("Deleted image from Firebase: {ImageUrl}", fileName);
                        }
                        else
                        {
                            _logger.Warning("Failed to delete image from Firebase: {ImageUrl}", fileName);
                        }
                    }
                    catch (Exception deleteEx)
                    {
                        // Log but don't fail the operation
                        _logger.Error(
                            deleteEx,
                            "Error deleting image {FileName} from Firebase (non-critical)",
                            fileName
                        );
                    }
                }
            }

            #endregion
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during database transaction, rolling back");

            // Rollback uploaded files
            await CleanupUploadedFiles(uploadedFileNames, cancellationToken);

            throw;
        }

        #endregion

        #region Redis Cache Invalidation

        var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
        );

        var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Products), existingProduct.Id.ToString())}:{role}:{brandId.ToString()}"
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Products), existingProduct.Id.ToString())}:{role}:{brandId.ToString()}"
            ),
            entityCachePrefix:
            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Products), existingProduct.Id.ToString())}:{role}:{brandId.ToString()}"
        );

        if (cacheListResult.Success && cacheByIdResult.Success)
        {
            _logger.Information(
                "Updated product (ID: {Id}). Cache: {CacheListMessage}, {CacheDetailMessage}",
                existingProduct.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Updated product '{Id}' but cache invalidation failed: {CacheListMessage}, {CacheDetailMessage}",
                existingProduct.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }
        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Cập nhật sản phẩm thành công",
            Data = new
            {
                ProductId = existingProduct.Id,
                // ImagesCount = remainingImages.Count,
                AttributesCount = request.SideAttibutes?.Count ?? existingProduct.ProductSideAttributes.Count,
                ImagesDeleted = deletedFileNames.Count,
                ImagesAdded = uploadedFileNames.Count
            }
        };
    }

    /// <summary>
    /// Cleanup uploaded files from Firebase
    /// </summary>
    private async Task CleanupUploadedFiles(List<string> fileNames, CancellationToken cancellationToken)
    {
        foreach (var fileName in fileNames)
        {
            try
            {
                await _mediaService.DeleteFileAsync(fileName, cancellationToken);
                _logger.Information("Cleaned up uploaded file: {FileName}", fileName);
            }
            catch (Exception deleteEx)
            {
                _logger.Error(
                    deleteEx,
                    "Failed to cleanup uploaded file {FileName}",
                    fileName
                );
            }
        }
    }

    // /// <summary>
    // /// Validate image file
    // /// </summary>
    // private bool IsValidImageFile(IFormFile file)
    // {
    //     var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    //     var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    //
    //     return file.Length > 0
    //            && file.Length <= 5 * 1024 * 1024 // 5MB
    //            && allowedExtensions.Contains(extension);
    // }
}