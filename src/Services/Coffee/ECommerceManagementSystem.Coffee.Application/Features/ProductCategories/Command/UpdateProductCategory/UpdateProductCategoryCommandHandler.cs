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

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.UpdateProductCategory;

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public UpdateProductCategoryCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, ILogger logger, IMapper mapper, IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(UpdateProductCategoryCommand request,
        CancellationToken cancellationToken)
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

        var existingCategory = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.Id && x.BrandId == brandId,
                include: q => q.Include(c => c.Products)
            );

        if (existingCategory == null)
        {
            throw new BadHttpRequestException("Danh mục không tồn tại");
        }

        // Không cho phép thay đổi status nếu có sản phẩm active
        if (request.Status == ECategoryStatus.Inactive &&
            existingCategory.Products?.Any(p => p.Status == EProductStatus.Active) == true)
        {
            throw new BadHttpRequestException(
                "Không thể vô hiệu hóa danh mục khi còn sản phẩm đang hoạt động"
            );
        }

        var isStatusChanged = existingCategory.Status != request.Status;
        
        existingCategory.Name = request.Name;
        existingCategory.Description = request.Description;
        existingCategory.DisplayOrder = request.DisplayOrder;
        // existingCategory.ImageUrl = request.ImageUrl;
        existingCategory.Status = request.Status;
        existingCategory.LastModifiedDate = DateTime.UtcNow;
        string uploadedFileName = "";
        string oldLogoUrl = existingCategory.ImageUrl;

        if (request.Image != null)
        {
            try
            {
                if (!ImageUtil.IsValidImageFile(request.Image))
                {
                    throw new BadHttpRequestException(
                        $"Logo không hợp lệ. " +
                        $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                    );
                }

                using var memoryStream = new MemoryStream();
                await request.Image.CopyToAsync(memoryStream, cancellationToken);
                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Image,
                    folderPath: nameof(Domain.Entities.Brands).ToLowerInvariant(),
                    cancellationToken
                );

                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                {
                    throw new Exception($"Không thể upload logo: {uploadResult.Message}");
                }

                uploadedFileName = uploadResult.FileName;
                existingCategory.ImageUrl = uploadResult.FileName;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to upload logo for brand {BrandId}", request.Id);
                await _unitOfWork.RollbackTransactionAsync();

                try
                {
                    if (!string.IsNullOrEmpty(uploadedFileName))
                    {
                        await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                        _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.Error(deleteEx, "Failed to delete image {FileName} during rollback", uploadedFileName);
                }

                throw new Exception($"Không thể upload logo: {e.Message}");
            }
        }

        _unitOfWork.GetRepository<Domain.Entities.ProductCategories>().UpdateAsync(existingCategory);
        
        var isSuccess = _unitOfWork.Commit() > 0;
        if (!isSuccess)
        {
            try
            {
                if (!string.IsNullOrEmpty(uploadedFileName))
                {
                    await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                    _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
                }
            }
            catch (Exception deleteEx)
            {
                _logger.Error(deleteEx, "Failed to delete image {FileName} during rollback", uploadedFileName);
            }
            throw new Exception("Cập nhật danh mục sản phẩm không thành công!");
        }

        if (!string.IsNullOrEmpty(uploadedFileName) && !string.IsNullOrEmpty(oldLogoUrl))
        {
            try
            {
                await _mediaService.DeleteFileAsync(oldLogoUrl, cancellationToken);
                _logger.Information("Deleted old logo: {FileName}", oldLogoUrl);
            }
            catch (Exception deleteEx)
            {
                _logger.Warning(deleteEx, "Failed to delete old logo {FileName}", oldLogoUrl);
            }
        }
    
        var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}")
            ),
            operation: isStatusChanged? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}")
        );

        var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.ProductCategories), existingCategory.Id.ToString())}:{role}:{brandId.ToString()}"
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.ProductCategories), existingCategory.Id.ToString())}:{role}:{brandId.ToString()}"
            ),
            entityCachePrefix:
            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.ProductCategories), existingCategory.Id.ToString())}:{role}:{brandId.ToString()}"
        );

        if (cacheListResult.Success && cacheByIdResult.Success)
        {
            _logger.Information(
                "Updated product category (ID: {Id}). Cache: {CacheListMessage}, {CacheDetailMessage}",
                existingCategory.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Updated product category '{Id}' but cache invalidation failed: {CacheListMessage}, {CacheDetailMessage}",
                existingCategory.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }
        
        // var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
        //     lockKey: CacheConfig.EntityInvalidationLock(nameof(ProductCategories)),
        //     operation: isStatusChanged? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
        //     counterKey: CacheConfig.EntityInvalidationCounter(nameof(Domain.Entities.ProductCategories)),
        //     entityCachePrefix: CacheConfig.EntityCachePrefix(nameof(Domain.Entities.ProductCategories))
        // );
        //
        // if (cacheResult.Success)
        // {
        //     _logger.Information(
        //         "Updated product category '{Name}' (ID: {Id}). Cache: {CacheMessage}",
        //         existingCategory.Name,
        //         existingCategory.Id,
        //         cacheResult.Message
        //     );
        // }
        // else
        // {
        //     _logger.Warning(
        //         "Updated product category '{Name}' but cache invalidation failed: {CacheMessage}",
        //         existingCategory.Name,
        //         cacheResult.Message
        //     );
        // }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Cập nhật danh mục sản phẩm thành công",
            Data = existingCategory.Id,
        };
    }
}