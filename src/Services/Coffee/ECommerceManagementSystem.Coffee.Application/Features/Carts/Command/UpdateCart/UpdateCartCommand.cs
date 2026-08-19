using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.UpdateCart;

/// <summary>
/// Command để update cart (hoặc tạo mới nếu chưa có)
/// </summary>
public class UpdateCartCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public Guid? CartId { get; set; }
    public string? CustomerNote { get; set; }
    public required List<UpdateCartItemRequest> Items { get; set; }
    public List<UpdateCartPromotionRequest>? AppliedPromotions { get; set; }
    public string? PromotionCodeToApply { get; set; }
}

public class UpdateCartItemRequest
{
    public required Guid ProductId { get; set; }
    public string? ProductImageUrlSnapshot { get; set; }
    public required int Quantity { get; set; }
}

public class UpdateCartPromotionRequest
{
    public required Guid PromotionRuleId { get; set; }
    public string PromotionRuleCode { get; set; }
    public required string PromotionRuleNameSnapshot { get; set; }
    public required decimal DiscountAmountApplied { get; set; }
}

