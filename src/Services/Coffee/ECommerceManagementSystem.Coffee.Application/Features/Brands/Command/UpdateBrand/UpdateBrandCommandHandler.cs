using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Common.Exceptions;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.UpdateBrand;

public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public UpdateBrandCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation,
        ILogger logger,
        IMapper mapper,
        IClaimService claimService,
        IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        #region Authentication

        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        #endregion

        #region Validate brand existence

        var existingBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
            predicate: x => x.Id == request.Id
        );

        if (existingBrand == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại");
        }

        #endregion

        #region Update brand information

        var isStatusChanged = (existingBrand.Status != request.Status);
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
        {
            _logger.Error($"Failed to begin transaction: {beginResult.Message}");
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = $"Failed to begin transaction: {beginResult.Message}",
            };
        }

        // Update brand properties
        existingBrand.Name = request.Name;
        existingBrand.Fullname = request.Fullname;
        existingBrand.Slogan = request.Slogan;
        existingBrand.Email = request.Email;
        existingBrand.Address = request.Address;
        existingBrand.PhoneNumber = request.PhoneNumber;
        existingBrand.Configuration = request.Configuration;
        existingBrand.Status = request.Status;
        existingBrand.LastModifiedDate = DateTime.UtcNow;

        #endregion

        #region Handle logo upload

        string uploadedFileName = "";
        string oldLogoUrl = existingBrand.LogoUrl;

        if (request.Logo != null)
        {
            try
            {
                if (!ImageUtil.IsValidImageFile(request.Logo))
                {
                    throw new BadHttpRequestException(
                        $"Logo không hợp lệ. " +
                        $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                    );
                }

                using var memoryStream = new MemoryStream();
                await request.Logo.CopyToAsync(memoryStream, cancellationToken);
                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Logo,
                    folderPath: nameof(Domain.Entities.Brands).ToLowerInvariant(),
                    cancellationToken
                );

                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                {
                    throw new Exception($"Không thể upload logo: {uploadResult.Message}");
                }

                uploadedFileName = uploadResult.FileName;
                existingBrand.LogoUrl = uploadResult.FileName;
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

        #endregion

        #region Commit transaction

        _unitOfWork.GetRepository<Domain.Entities.Brands>().UpdateAsync(existingBrand);

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            if (commitResult.ValidationErrors?.Any() == true)
            {
                foreach (var error in commitResult.ValidationErrors)
                {
                    _logger.Warning(
                        $"Validation Error - {string.Join(", ", error.MemberNames)}: {error.ErrorMessage}");
                }
            }
            else
            {
                _logger.Error($"Transaction failed: {commitResult.Message}", commitResult.Exception);
            }

            // Rollback uploaded file if transaction fails
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

            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể cập nhật thương hiệu!");
        }

        // Delete old logo if new one was uploaded successfully
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

        #endregion

        #region Cache invalidation

        _logger.Information("Cập nhật thương hiệu thành công: {BrandId}", existingBrand.Id);

        // Invalidate cache (sau khi commit thành công)
        // Thương hiệu sau khi cập nhật thành công nếu critical
        // cần xóa tất cả trong list brand ở redis trước đó, bao gồm detail.
        var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
            ),
            operation: isStatusChanged ? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
            ),
            entityCachePrefix: CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
        );

        var cacheDetailResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Brands), existingBrand.Id.ToString())
            ),
            operation: isStatusChanged ? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Brands), existingBrand.Id.ToString())
            ),
            entityCachePrefix: CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Brands),
                existingBrand.Id.ToString())
        );
        
        var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Brands), existingBrand.Id.ToString())
            ),
            operation: isStatusChanged ? EOperationBeforeCache.BulkUpdate : EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Brands), existingBrand.Id.ToString())
            ),
            entityCachePrefix: CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Brands),
                existingBrand.Id.ToString())
        );

        if (cacheListResult.Success && cacheDetailResult.Success && cacheByIdResult.Success)
        {
            _logger.Information(
                $"Updated brand '{existingBrand.Name}' (ID: {existingBrand.Id}). Cache: {cacheListResult.Message}, {cacheDetailResult.Message}, {cacheByIdResult.Message}."
            );
        }
        else
        {
            _logger.Warning(
                $"Updated brand '{existingBrand.Name}' but cache invalidation failed: {cacheListResult.Message}, {cacheDetailResult.Message}, {cacheByIdResult.Message}."
            );
        }

        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Cập nhật thương hiệu thành công",
            Data = existingBrand.Id
        };
    }
}