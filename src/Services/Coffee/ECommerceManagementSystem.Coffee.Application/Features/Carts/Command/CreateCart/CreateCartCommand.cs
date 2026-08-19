using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.CreateCart;

public class CreateCartCommand : IRequest<ApiResponse>
{
    /// <summary>
    /// CartId - Optional. Nếu null thì tạo cart mới, nếu có thì get cart đó
    /// </summary>
    public Guid? CartId { get; set; }
    
    /// <summary>
    /// Tên cart (optional) - VD: "Giỏ hàng chính", "Giỏ hàng mua sau"
    /// </summary>
    public string? CartName { get; set; }
    public required string TimeZone {get;set;}
}

