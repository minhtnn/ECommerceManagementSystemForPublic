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

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;

public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public CreateBrandCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
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

    public async ValueTask<ApiResponse> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
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

        #region Validate brand information

        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
            predicate: x => x.Code.Equals(request.Code.Trim())
        );
        if (existedBrand != null)
        {
            throw new BadHttpRequestException("Mã thương hiệu đã tồn tại");
        }

        var existedAccount = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
            predicate: x => (x.Email.Equals(request.Email.Trim()) ||
                             (x.Username.Equals(request.Username.Trim())))
        );
        if (existedAccount != null)
        {
            throw new BadHttpRequestException("Email thương hiệu đã tồn tại");
        }

        #endregion

        #region Insert new brand and relevants

        // 2. Map the brand create request to entity 
        var brand = _mapper.Map<Domain.Entities.Brands>(request);
        brand.Id = Guid.CreateVersion7();
        brand.Status = EBrandStatus.Active;
        brand.CreatedDate = DateTime.UtcNow;
        brand.LastModifiedDate = DateTime.UtcNow;
        // 3. Create account for brand
        var (passwordHash, passwordSalt) = AuthenUtil.HashPassword(request.PasswordString);
        var account = new Accounts()
        {
            Id = Guid.CreateVersion7(),
            Role = ERole.BrandAdmin,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Status = EAccountStatus.Active,
            CreatedDate = brand.CreatedDate,
            LastModifiedDate = brand.LastModifiedDate,
        };
        var brandAccount = new BrandAccounts()
        {
            Id = Guid.CreateVersion7(),
            BrandId = brand.Id,
            AccountId = account.Id,
            CreatedDate = DateTime.UtcNow,
        };
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
        {
            _logger.Error($"Failed to begin transaction: {beginResult.Message}");
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = $"Failed to begin transaction: {beginResult.Message}",
            };
            ;
        }

        await _unitOfWork.GetRepository<Domain.Entities.Brands>().InsertAsync(brand);
        await _unitOfWork.GetRepository<Accounts>().InsertAsync(account);
        await _unitOfWork.GetRepository<BrandAccounts>().InsertAsync(brandAccount);

        #endregion

        #region Check if brand image are contained in request

        string uploadedFileName = "";

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
                    folderPath: nameof(Domain.Entities.Brands)
                        .ToLowerInvariant(),
                    cancellationToken
                );
                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                {
                    throw new Exception(
                        $"Không thể upload logo: {uploadResult.Message}"
                    );
                }

                uploadedFileName = uploadResult.FileName;
                brand.LogoUrl = uploadResult.FileName;
            }
            catch (Exception e)
            {
                try
                {
                    await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                    _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
                }
                catch (Exception deleteEx)
                {
                    _logger.Error(
                        deleteEx,
                        "Failed to delete image {FileName} during rollback",
                        uploadedFileName
                    );
                }
            }
        }

        #endregion

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

            try
            {
                await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
            }
            catch (Exception deleteEx)
            {
                _logger.Error(
                    deleteEx,
                    "Failed to delete image {FileName} during rollback",
                    uploadedFileName
                );
            }

            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể tạo thương hiệu!");
        }

        if (commitResult.RowsAffected < 3)
        {
            _logger.Warning($"Expected 3 rows but only {commitResult.RowsAffected} were affected");
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể tạo thương hiệu!");
        }

        _logger.Information("Tạo thương hiệu thành công: {BrandId}", brand.Id);

        // Invalidate cache (sau khi commit thành công)
        // Thương hiệu sau khi tạo thành công cần xóa tất cả trong list brand ở redis trước đó, không bao gồm detail.
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
            ),
            entityCachePrefix: CacheConfig.EntityListCachePrefix(nameof(Domain.Entities.Brands))
        );
        if (cacheResult.Success)
        {
            _logger.Information(
                $"Created brand '{brand.Name}' (ID: {brand.Id}). Cache: {cacheResult.Message}"
            );
        }
        else
        {
            _logger.Warning(
                $"Created brand '{brand.Name}' but cache invalidation failed: {cacheResult.Message}"
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo mới thương hiệu thành công",
            Data = brand.Id
        };
    }
}