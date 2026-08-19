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
using Google.Protobuf;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Command.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IMediaService _mediaService;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;

    public CreateProductCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, ILogger logger, IMapper mapper, IMediaService mediaService,
        IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _mediaService = mediaService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
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

        #region Check if product code exists

        var existingProduct = await _unitOfWork.GetRepository<Domain.Entities.Products>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.Code)
            );
        if (existingProduct != null)
        {
            throw new BadHttpRequestException("Mã sản phẩm đã tồn tại");
        }

        #endregion

        #region Check product category's validation

        var productCategory = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.ProductCategoryId,
                include: q => q.Include(c => c.Brand)
            );

        if (productCategory == null)
        {
            throw new BadHttpRequestException("Danh mục sản phẩm không tồn tại");
        }

        // Quan trọng: Validate category phải là leaf (IsLeafOnly = true)
        if (!productCategory.IsLeafOnly)
        {
            throw new BadHttpRequestException(
                "Chỉ có thể gán sản phẩm vào danh mục cấp thấp nhất (leaf category). " +
                $"Danh mục '{productCategory.Name}' có danh mục con bên trong."
            );
        }

        // Validate category status
        if (productCategory.Status != ECategoryStatus.Active)
        {
            throw new BadHttpRequestException(
                $"Không thể tạo sản phẩm trong danh mục không hoạt động. " +
                $"Trạng thái danh mục: {productCategory.Status}"
            );
        }

        if (productCategory.Brand == null || productCategory.Brand.Status != EBrandStatus.Active)
        {
            throw new BadHttpRequestException("Thương hiệu không hoạt động");
        }

        #endregion

        #region Insert new product

        var product = _mapper.Map<Domain.Entities.Products>(request);
        product.Id = Guid.CreateVersion7();
        product.CreatedDate = DateTime.UtcNow;
        product.LastModifiedDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(product.FullName))
        {
            product.FullName = product.Name;
        }

        await _unitOfWork.GetRepository<Domain.Entities.Products>().InsertAsync(product);

        #endregion

        #region Check if product images are contained in request

        if (request.ImageFiles != null && request.ImageFiles.Count() > 4)
        {
            throw new Exception("Chỉ lưu tối đa 4 ảnh!");
        }

        var uploadedFileNames = new List<string>();
        if (request.ImageFiles != null && request.ImageFiles.Any())
        {
            _logger.Information("Starting upload {Count} images to Firebase", request.ImageFiles.Count);
            var metadata = request.CreateProductImageMetadata ?? new List<CreateProductImageMetadata>();
            try
            {
                for (int i = 0; i < request.ImageFiles.Count; i++)
                {
                    var file = request.ImageFiles[i];
                    if (!ImageUtil.IsValidImageFile(file))
                    {
                        throw new BadHttpRequestException(
                            $"File ảnh thứ {i + 1} không hợp lệ. " +
                            $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                        );
                    }

                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream, cancellationToken);
                    var byteString = ByteString.CopyFrom(memoryStream.ToArray());
                    var uploadResult = await _mediaService.UploadImageFromFormAsync(
                        file,
                        folderPath: nameof(Domain.Entities.Products)
                            .ToLowerInvariant(),
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
                    var productImage = new ProductImages
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = product.Id,
                        ImageUrl = uploadResult.FileName,
                        AltText = imageMeta?.AltText,
                        IsMainImage = imageMeta?.IsMainImage ?? false,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };
                    product.ProductImages.Add(productImage);

                    _logger.Information(
                        "Uploaded image {Index}/{Total}: {FileName}",
                        i + 1,
                        request.ImageFiles.Count,
                        uploadResult.FileName
                    );
                }

                if (product.ProductImages.Any() &&
                    !product.ProductImages.Any(x => x.IsMainImage))
                {
                    product.ProductImages.First().IsMainImage = true;
                    _logger.Information("Set first image as main image");
                }

                _logger.Information(
                    "Successfully uploaded all {Count} images to Firebase",
                    product.ProductImages.Count
                );
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error uploading images, rolling back uploaded files");

                foreach (var fileName in uploadedFileNames)
                {
                    try
                    {
                        await _mediaService.DeleteFileAsync(fileName, cancellationToken);
                        _logger.Information("Deleted rolled back image: {FileName}", fileName);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.Error(
                            deleteEx,
                            "Failed to delete image {FileName} during rollback",
                            fileName
                        );
                    }
                }
            }
        }

        #endregion

        #region Check if product's side attributes are contained in request

        if (request.SideAttibutes != null && request.SideAttibutes.Any())
        {
            foreach (var attr in request.SideAttibutes)
            {
                var sideAttribute = new ProductSideAttributes
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = product.Id,
                    Key = attr.Key,
                    Value = attr.Value,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                product.ProductSideAttributes.Add(sideAttribute);
            }

            _logger.Information(
                "Created {Count} side attributes for product",
                product.ProductSideAttributes.Count
            );
        }

        #endregion

        #region Insert into database logic

        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );

            // Rollback Firebase images nếu commit database thất bại
            foreach (var fileName in uploadedFileNames)
            {
                try
                {
                    await _mediaService.DeleteFileAsync(fileName, cancellationToken);
                    _logger.Information("Deleted image after commit failed: {FileName}", fileName);
                }
                catch (Exception deleteEx)
                {
                    _logger.Error(
                        deleteEx,
                        "Failed to delete image {FileName} after commit failed",
                        fileName
                    );
                }
            }

            throw new Exception($"Không thể tạo sản phẩm: {commitResult.Message}");
        }

        #endregion

        #region Redis logic

        var productCacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Products)}:{role}:{brandId.ToString()}")
        );

        var categoryCacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}"
                )
            ),
            operation: EOperationBeforeCache.NormalUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}")),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix(
                $"{nameof(Domain.Entities.ProductCategories)}:{role}:{brandId.ToString()}")
        );
        if (productCacheResult.Success && categoryCacheResult.Success)
        {
            _logger.Information(
                "Created product '{Name}' (ID: {Id}). Cache invalidated successfully",
                product.Name,
                product.Id
            );
        }
        else
        {
            _logger.Warning(
                "Created product '{Name}' but cache invalidation had issues. " +
                "Product: {ProductCache}, Category: {CategoryCache}",
                product.Name,
                productCacheResult.Message,
                categoryCacheResult.Message
            );
        }

        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo mới sản phẩm thành công",
            Data = product.Id,
        };
    }
}