using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart;
using ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.CreateCart;

public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, ApiResponse>
{
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    // private static readonly TimeSpan CartCacheExpiry = TimeSpan.FromDays(30);
    private const int MaxCartsPerCustomer = 1;
    private const string MetadataField = "metadata";
    

    public CreateCartCommandHandler(ILogger logger, IRedisService redisService, IClaimService claimService)
    {
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var customerId = _claimService.GetCurrentReferenceId();
        if (role == null || role != ERole.EndCustomer || customerId == null || customerId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }
        var hashKey = BuildHashKey(customerId);

        // Nếu có CartId -> Get cart cụ thể
        if (request.CartId.HasValue && request.CartId.Value != Guid.Empty)
        {
            return await GetExistingCart(hashKey, request, customerId);
        }
        
        return await CreateNewCart(hashKey, customerId, request);
    }
    
     private async Task<ApiResponse> GetExistingCart(string hashKey, CreateCartCommand request, Guid customerId)
    {
        var cartField = BuildCartField(request.CartId.Value);
        var existingCartJson = await _redisService.GetHashAsync(hashKey, cartField);

        if (string.IsNullOrEmpty(existingCartJson))
        {
            _logger.Warning("Cart {CartId} not found for customer {CustomerId}", request.CartId.Value, customerId);
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy giỏ hàng!"
            };
        }

        var cart = JsonSerializer.Deserialize<GetCustomerCartResponse>(existingCartJson);
        
        _logger.Information("Retrieved cart {CartId} for customer {CustomerId}", request.CartId.Value, customerId);
        if (cart != null)
        {
            cart.CreatedDate = TimeUtil.ConvertFromUtc(cart.CreatedDate, request.TimeZone);
            cart.LastModifiedDate = TimeUtil.ConvertFromUtc(cart.LastModifiedDate, request.TimeZone);
        }
        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy giỏ hàng thành công!",
            Data = cart
        };
    }

    private async Task<ApiResponse> CreateNewCart(string hashKey, Guid customerId, CreateCartCommand request)
    {
        // Kiểm tra số lượng carts hiện tại
        var metadata = await GetMetadata(hashKey);
        
        if (metadata.CartCount >= MaxCartsPerCustomer)
        {
            _logger.Warning("Customer {CustomerId} reached maximum cart limit", customerId);
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = $"Bạn chỉ có thể tạo tối đa {MaxCartsPerCustomer} giỏ hàng!"
            };
        }

        // Tạo cart mới
        var cartId = Guid.CreateVersion7();
        var isFirstCart = metadata.CartCount == 0;
        
        var newCart = new GetCustomerCartResponse
        {
            Id = cartId,
            CustomerId = customerId,
            CartName = request.CartName ?? (isFirstCart ? "Giỏ hàng chính" : $"Giỏ hàng {metadata.CartCount + 1}"),
            IsActive = isFirstCart, // Cart đầu tiên sẽ là active
            TotalAmountWithoutDiscount = 0,
            TotalOrderDiscount = 0,
            TotalOrderShippingFee = 0,
            TotalAmount = 0,
            CustomerNote = null,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            Items = new List<GetCustomerCartItemsResponse>(),
            AppliedPromotions = new List<GetCustomerCartAppliedPromotionsResponse>()
        };

        // Save cart vào Hash
        var cartField = BuildCartField(cartId);
        var cartJson = JsonSerializer.Serialize(newCart);
        await _redisService.SetHashAsync(hashKey, cartField, cartJson);

        // Update metadata
        metadata.CartCount++;
        if (isFirstCart)
        {
            metadata.ActiveCartId = cartId;
        }
        var metadataJson = JsonSerializer.Serialize(metadata);
        await _redisService.SetHashAsync(hashKey, MetadataField, metadataJson);

        // Set expiry cho hash key
        // await _redisService.SetExpireAsync(hashKey, CartCacheExpiry);

        _logger.Information("Cart {CartId} created successfully for customer {CustomerId}", cartId, customerId);
        newCart.CreatedDate = TimeUtil.ConvertFromUtc(newCart.CreatedDate, request.TimeZone);
        newCart.LastModifiedDate = TimeUtil.ConvertFromUtc(newCart.LastModifiedDate, request.TimeZone);
        return new ApiResponse
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo giỏ hàng thành công!",
            Data = newCart
        };
    }

    private async Task<CartMetadata> GetMetadata(string hashKey)
    {
        var metadataJson = await _redisService.GetHashAsync(hashKey, MetadataField);
        
        if (string.IsNullOrEmpty(metadataJson))
        {
            return new CartMetadata();
        }

        return JsonSerializer.Deserialize<CartMetadata>(metadataJson) ?? new CartMetadata();
    }

    private string BuildHashKey(Guid customerId)
    {
        return $"{CacheConfig.EntityListCachePrefix("carts")}:{customerId}";
    }

    private string BuildCartField(Guid cartId)
    {
        return $"cart:{cartId}";
    }
    
}