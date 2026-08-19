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

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;

public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;

    public CreateProductCategoryCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
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

    public async ValueTask<ApiResponse> Handle(CreateProductCategoryCommand request,
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

        // Bắt đầu transaction
        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Không thể bắt đầu transaction: {Message}", transactionResult.Message);
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        // 1. Validate code uniqueness
        var existedProductCategory = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.Code)
                                && x.BrandId == brandId
            );
        if (existedProductCategory != null)
        {
            throw new BadHttpRequestException("Mã danh mục sản phẩm đã tồn tại");
        }

        // 2. Validate brand exists
        var brandExists = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                selector: x => x.Id,
                predicate: x => x.Id == brandId
            );
        if (brandExists == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại");
        }

        int level = 0;
        Domain.Entities.ProductCategories? parentCategory = null;

        // 3. Handle parent category
        if (request.ParentProductCategoryId != null && request.ParentProductCategoryId.HasValue)
        {
            parentCategory = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
                .SingleOrDefaultAsync(
                    predicate: x => x.Id == request.ParentProductCategoryId.Value,
                    include: x => x.Include(x => x.Products)
                );

            if (parentCategory == null)
            {
                throw new BadHttpRequestException("Không tồn tại danh mục sản phẩm cha!");
            }

            if (parentCategory.Products.Any())
            {
                throw new BadHttpRequestException("Danh mục cha đã được gán sản phẩm!");
            }

            // Tính level từ parent
            level = parentCategory.Level + 1;

            // Cập nhật parent: không còn là leaf nữa
            if (parentCategory.IsLeafOnly)
            {
                parentCategory.IsLeafOnly = false;
                parentCategory.IsDeletable = false;
                parentCategory.LastModifiedDate = DateTime.UtcNow;
                _unitOfWork.GetRepository<Domain.Entities.ProductCategories>().UpdateAsync(parentCategory);

                _logger.Information(
                    "Cập nhật parent category {ParentId} - IsLeafOnly = false",
                    parentCategory.Id
                );
            }
        }

        // 4. Tạo category mới
        var productCategory = _mapper.Map<Domain.Entities.ProductCategories>(request);
        productCategory.Id = Guid.CreateVersion7();
        productCategory.BrandId = brandId;
        productCategory.CreatedDate = DateTime.UtcNow;
        productCategory.LastModifiedDate = DateTime.UtcNow;
        productCategory.Level = level;
        productCategory.IsLeafOnly = true;
        productCategory.IsDeletable = true;

        await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .InsertAsync(productCategory);
        string uploadedFileName = "";
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
                    folderPath: nameof(Domain.Entities.ProductCategories)
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
                productCategory.ImageUrl = uploadResult.FileName;
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

        // 5. Commit transaction
        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
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
            throw new Exception($"Không thể tạo danh mục sản phẩm: {commitResult.Message}");
        }

        _logger.Information(
            "Tạo danh mục sản phẩm thành công: {ProductId} với Level: {Level}. Rows affected: {RowsAffected}",
            productCategory.Id,
            productCategory.Level,
            commitResult.RowsAffected
        );

        // 6. Invalidate cache (sau khi commit thành công)
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId}")
        );

        if (cacheResult.Success)
        {
            _logger.Information(
                "Created product category '{Name}' (ID: {Id}). Cache: {CacheMessage}",
                productCategory.Name,
                productCategory.Id,
                cacheResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Created product category '{Name}' but cache invalidation failed: {CacheMessage}",
                productCategory.Name,
                cacheResult.Message
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo mới danh mục sản phẩm thành công",
            Data = productCategory.Id,
        };
    }
}