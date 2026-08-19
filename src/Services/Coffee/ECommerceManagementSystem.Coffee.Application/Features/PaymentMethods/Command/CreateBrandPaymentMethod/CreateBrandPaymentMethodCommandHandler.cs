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
using System.Text.Json;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;

public class CreateBrandPaymentMethodCommandHandler : IRequestHandler<CreateBrandPaymentMethodCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;

    public CreateBrandPaymentMethodCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, ILogger logger, IMapper mapper, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _mapper = mapper;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(CreateBrandPaymentMethodCommand request,
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

        var existingBrandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .SingleOrDefaultAsync(
                predicate: x => x.PaymentMethodId == request.PaymentMethodId && x.BrandId == brandId
            );
        if (existingBrandPaymentMethod != null)
        {
            throw new BadHttpRequestException("Thương hiệu đã gán phương thức thanh toán này trước đó!");
        }

        var existingPaymentMethod = await _unitOfWork.GetRepository<Domain.Entities.PaymentMethods>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.PaymentMethodId
            );
        if (existingPaymentMethod == null)
        {
            throw new BadHttpRequestException("Phương thức thanh toán không tồn tại!");
        }

        if (existingPaymentMethod.Status != EPaymentMethodStatus.Active)
        {
            throw new BadHttpRequestException("Phương thức thanh toán này đang không khả dụng!");
        }

        if (request.IsDefault)
        {
            var currentDefault = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                .SingleOrDefaultAsync(
                    predicate: x => x.BrandId == brandId && x.IsDefault
                );

            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                _unitOfWork.GetRepository<BrandPaymentMethods>().UpdateAsync(currentDefault);
            }
        }

        // Filter Configuration dựa trên ConfigurationSchema của PaymentMethod
        var allowedKeys = ParseAllowedKeys(existingPaymentMethod.ConfigurationSchema);
        var filteredConfiguration = FilterConfiguration(request.Configuration, allowedKeys);

        var newBrandPaymentMethod = _mapper.Map<BrandPaymentMethods>(request);
        newBrandPaymentMethod.BrandId = brandId;
        newBrandPaymentMethod.Id = Guid.CreateVersion7();
        newBrandPaymentMethod.CreatedDate = DateTime.UtcNow;
        newBrandPaymentMethod.Configuration = filteredConfiguration; // Override bằng filtered version

        await _unitOfWork.GetRepository<BrandPaymentMethods>().InsertAsync(newBrandPaymentMethod);
        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            throw new Exception($"Không thể gán phương thức thanh toán: {commitResult.Message}");
        }

        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix(
                    $"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(BrandPaymentMethods)}:{ERole.BrandAdmin}:{brandId.ToString()}")
        );

        if (cacheResult.Success)
        {
            _logger.Information(
                "Created brand payment method (ID: {Id}). Cache: {CacheMessage}",
                newBrandPaymentMethod.Id,
                cacheResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Created brand payment method '{Id}' but cache invalidation failed: {CacheMessage}",
                newBrandPaymentMethod.Id,
                cacheResult.Message
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Gán phương thức thanh toán thành công!",
            Data = newBrandPaymentMethod.Id
        };
    }

    /**
     * Parse ConfigurationSchema (comma-separated) thành list of allowed keys.
     * "name, age, email" → ["name", "age", "email"]
     */
    private static List<string> ParseAllowedKeys(string? configurationSchema)
    {
        if (string.IsNullOrWhiteSpace(configurationSchema)) return new List<string>();
        return configurationSchema
            .Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();
    }

    /**
     * Filter Configuration JSON: chỉ giữ keys thuộc allowedKeys.
     * Input:  {"name": "Minh", "age": "13", "hack": "bad"}
     * Output: {"name": "Minh", "age": "13"}  (key "hack" bị loại)
     *
     * Nếu key thuộc allowedKeys nhưng không có trong input → default rỗng string.
     */
    private static string? FilterConfiguration(string? configuration, List<string> allowedKeys)
    {
        if (allowedKeys.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(configuration))
        {
            // Không có configuration gửi lên → build object với tất cả keys rỗng
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
                    filtered[key] = ""; // default rỗng nếu không gửi
            }

            return JsonSerializer.Serialize(filtered);
        }
        catch
        {
            // Nếu parse JSON fail → return null, không save configuration
            return null;
        }
    }
}