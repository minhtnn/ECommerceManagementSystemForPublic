using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdateBrandPaymentMethod;

public class UpdateBrandPaymentMethodCommandHandler : IRequestHandler<UpdateBrandPaymentMethodCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;

    public UpdateBrandPaymentMethodCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation,
        ILogger logger,
        IMapper mapper,
        IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _claimService = claimService;
    }

    private static List<string> ParseAllowedKeys(string? configurationSchema)
    {
        if (string.IsNullOrWhiteSpace(configurationSchema)) return new List<string>();
        return configurationSchema
            .Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();
    }

    private static string? FilterConfiguration(string? configuration, List<string> allowedKeys)
    {
        if (allowedKeys.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(configuration))
        {
            var empty = allowedKeys.ToDictionary(k => k, _ => "");
            return JsonSerializer.Serialize(empty);
        }

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var filtered = new Dictionary<string, string>();

            foreach (var key in allowedKeys)
            {
                if (doc.RootElement.TryGetProperty(key, out var value))
                    filtered[key] = value.GetString() ?? "";
                else
                    filtered[key] = "";
            }

            return JsonSerializer.Serialize(filtered);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask<ApiResponse> Handle(UpdateBrandPaymentMethodCommand request,
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

        try
        {
            var existingBrandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                .SingleOrDefaultAsync(
                    predicate: x => x.Id == request.Id && x.BrandId == brandId,
                    include: i => i.Include(x => x.PaymentMethods)
                );

            if (existingBrandPaymentMethod == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new BadHttpRequestException("Không tìm thấy phương thức thanh toán của thương hiệu!");
            }

            if (request.IsActive && existingBrandPaymentMethod.PaymentMethods?.Status != EPaymentMethodStatus.Active)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new BadHttpRequestException(
                    "Không thể kích hoạt phương thức thanh toán này vì nó đã bị vô hiệu hóa ở cấp hệ thống!"
                );
            }

            if (request.IsDefault && !existingBrandPaymentMethod.IsDefault)
            {
                var currentDefault = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.BrandId == brandId && x.IsDefault && x.Id != request.Id
                    );

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    _unitOfWork.GetRepository<BrandPaymentMethods>().UpdateAsync(currentDefault);
                }
            }

            // Filter Configuration dựa trên ConfigurationSchema của PaymentMethod (parent)
            var allowedKeys = ParseAllowedKeys(existingBrandPaymentMethod.PaymentMethods?.ConfigurationSchema);
            var filteredConfiguration = FilterConfiguration(request.Configuration, allowedKeys);

            existingBrandPaymentMethod.IsDefault = request.IsDefault;
            existingBrandPaymentMethod.DisplayOrder = request.DisplayOrder;
            existingBrandPaymentMethod.IsActive = request.IsActive;
            existingBrandPaymentMethod.Configuration = filteredConfiguration; // Override bằng filtered version
            existingBrandPaymentMethod.LastModifiedDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<BrandPaymentMethods>().UpdateAsync(existingBrandPaymentMethod);

            var commitResult = await _unitOfWork.CommitTransactionAsync();

            if (!commitResult.IsSuccess)
            {
                _logger.Error(
                    "Transaction commit failed: {Message}. Exception: {Exception}",
                    commitResult.Message,
                    commitResult.Exception?.Message
                );
                throw new Exception($"Không thể cập nhật phương thức thanh toán: {commitResult.Message}");
            }

            var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                lockKey: CacheConfig.EntityInvalidationLock(
                    CacheConfig.EntityListCachePrefix(
                        $"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
                ),
                operation: EOperationBeforeCache.BulkUpdate,
                counterKey: CacheConfig.EntityInvalidationCounter(
                    CacheConfig.EntityListCachePrefix(
                        $"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
                ),
                entityCachePrefix:
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
            );

            var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                lockKey: CacheConfig.EntityInvalidationLock(
                    $"{CacheConfig.EntityByIdCachePrefix(nameof(BrandPaymentMethods), existingBrandPaymentMethod.Id.ToString())}:{ERole.BrandAdmin}:{brandId.ToString()}"
                ),
                operation: EOperationBeforeCache.BulkUpdate,
                counterKey: CacheConfig.EntityInvalidationCounter(
                    $"{CacheConfig.EntityByIdCachePrefix(nameof(BrandPaymentMethods), existingBrandPaymentMethod.Id.ToString())}:{ERole.BrandAdmin}:{brandId.ToString()}"
                ),
                entityCachePrefix:
                $"{CacheConfig.EntityByIdCachePrefix(nameof(BrandPaymentMethods), existingBrandPaymentMethod.Id.ToString())}:{ERole.BrandAdmin}:{brandId.ToString()}"
            );

            if (cacheListResult.Success && cacheByIdResult.Success)
            {
                _logger.Information(
                    "Updated brand payment method (ID: {Id}). Cache: {CacheListMessage}, {CacheDetailMessage}",
                    existingBrandPaymentMethod.Id,
                    cacheListResult.Message,
                    cacheByIdResult.Message
                );
            }
            else
            {
                _logger.Warning(
                    "Updated brand payment method '{Id}' but cache invalidation failed: {CacheListMessage}, {CacheDetailMessage}",
                    existingBrandPaymentMethod.Id,
                    cacheListResult.Message,
                    cacheByIdResult.Message
                );
            }

            return new ApiResponse()
            {
                Status = StatusCodes.Status200OK,
                Message = "Cập nhật phương thức thanh toán thành công!",
                Data = existingBrandPaymentMethod.Id
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error(ex, "Error updating brand payment method: {Message}", ex.Message);
            throw;
        }
    }
}