using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Query.GetCustomerCart;

public class GetCustomerCartCommandHandler : IRequestHandler<GetCustomerCartCommand, ApiResponse>
{
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;

    private const string MetadataField = "metadata";

    public GetCustomerCartCommandHandler(
        ILogger logger,
        IRedisService redisService,
        IClaimService claimService)
    {
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetCustomerCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Role
        var role = _claimService.GetCurrentRoleEnum();
        if (role != ERole.EndCustomer)
        {
            _logger.Warning("Unauthorized cart access attempt by role: {Role}", role);
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Chỉ khách hàng mới có thể xem giỏ hàng!"
            };
        }

        // 2. Validate CustomerId
        var customerId = _claimService.GetCurrentReferenceId();
        if (customerId == Guid.Empty)
        {
            _logger.Error("Invalid customer ID");
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Thông tin khách hàng không hợp lệ!"
            };
        }

        var hashKey = BuildHashKey(customerId);

        // 3. Get metadata để lấy active cart ID
        var metadata = await GetMetadata(hashKey);

        if (metadata.CartCount == 0 || !metadata.ActiveCartId.HasValue)
        {
            _logger.Information("No cart found for customer {CustomerId}", customerId);
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Bạn chưa có giỏ hàng. Vui lòng tạo giỏ hàng mới!"
            };
        }

        // 4. Get cart từ Redis Hash
        var cartField = BuildCartField(metadata.ActiveCartId.Value);
        var cartJson = await _redisService.GetHashAsync(hashKey, cartField);

        if (string.IsNullOrEmpty(cartJson))
        {
            _logger.Warning(
                "Cart {CartId} referenced in metadata not found for customer {CustomerId}",
                metadata.ActiveCartId.Value, customerId);

            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy giỏ hàng. Vui lòng tạo giỏ hàng mới!"
            };
        }

        // 5. Deserialize cart
        var cart = JsonSerializer.Deserialize<GetCustomerCartResponse>(cartJson);

        if (cart == null)
        {
            _logger.Error("Failed to deserialize cart {CartId}", metadata.ActiveCartId.Value);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Lỗi xử lý dữ liệu giỏ hàng!"
            };
        }

        _logger.Information(
            "Retrieved cart {CartId} for customer {CustomerId}",
            cart.Id, customerId);

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy giỏ hàng thành công!",
            Data = cart
        };
    }

    /// <summary>
    /// Lấy metadata từ Redis Hash
    /// </summary>
    private async Task<CartMetadata> GetMetadata(string hashKey)
    {
        var metadataJson = await _redisService.GetHashAsync(hashKey, MetadataField);

        if (string.IsNullOrEmpty(metadataJson))
        {
            return new CartMetadata();
        }

        return JsonSerializer.Deserialize<CartMetadata>(metadataJson) ?? new CartMetadata();
    }

    /// <summary>
    /// Build hash key cho customer
    /// Format: carts:{customerId}
    /// </summary>
    private string BuildHashKey(Guid customerId)
    {
        return $"{CacheConfig.EntityListCachePrefix("carts")}:{customerId}";
    }

    /// <summary>
    /// Build cart field trong hash
    /// Format: cart:{cartId}
    /// </summary>
    private string BuildCartField(Guid cartId)
    {
        return $"cart:{cartId}";
    }
}