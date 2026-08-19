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

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;

public class CreatePaymentMethodCommandHandler : IRequestHandler<CreatePaymentMethodCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;
    private readonly IMapper _mapper;

    public CreatePaymentMethodCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IClaimService claimService, IMediaService mediaService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheInvalidation = cacheService;
        _claimService = claimService;
        _mediaService = mediaService;
        _mapper = mapper;
    }

    public async ValueTask<ApiResponse> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || role != ERole.SystemAdmin)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var existingPaymentMethod = await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code == request.Code
            );

        if (existingPaymentMethod != null)
        {
            throw new BadHttpRequestException("Phương thức thanh toán đã tồn tại!");
        }


        var paymentMethod = _mapper.Map<Domain.Entities.PaymentMethods>(request);
        paymentMethod.Id = Guid.CreateVersion7();
        paymentMethod.CreatedDate = DateTime.UtcNow;

        string uploadedFileName = "";

        if (request.Image != null)
        {
            try
            {
                if (!ImageUtil.IsValidImageFile(request.Image))
                {
                    throw new BadHttpRequestException(
                        $"Ảnh không hợp lệ. " +
                        $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                    );
                }

                using var memoryStream = new MemoryStream();
                await request.Image.CopyToAsync(memoryStream, cancellationToken);
                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Image,
                    folderPath: nameof(Domain.Entities.Brands)
                        .ToLowerInvariant(),
                    cancellationToken
                );
                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                {
                    throw new Exception(
                        $"Không thể upload ảnh: {uploadResult.Message}"
                    );
                }

                uploadedFileName = uploadResult.FileName;
                paymentMethod.ImageUrl = uploadResult.FileName;
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

        await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>().InsertAsync(paymentMethod);
        var isSuccessful = (await _unitOfWork.CommitAsync()) > 0;
        if (!isSuccessful)
        {
            throw new Exception("Đã có lỗi xảy ra!");
        }

        _logger.Information("Tạo phương thức thanh toán thành công: {BrandId}", paymentMethod.Id);
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.PaymentMethods)}:{role}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.PaymentMethods)}:{role}")
            ),
            entityCachePrefix: CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.PaymentMethods)}:{role}")
        );
        if (cacheResult.Success)
        {
            _logger.Information(
                $"Created payment method '{paymentMethod.Name}' (ID: {paymentMethod.Id}). Cache: {cacheResult.Message}"
            );
        }
        else
        {
            _logger.Warning(
                $"Created payment method '{paymentMethod.Name}' but cache invalidation failed: {cacheResult.Message}"
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo mới phương thức thanh toán thành công",
            Data = paymentMethod.Id
        };
    }
}