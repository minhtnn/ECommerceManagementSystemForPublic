using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class OrderHistoryStatus : EntityAuditBase<Guid>
{
    public required Guid OrderId { get; set; }
    public required EOrderStatus  FromStatus { get; set; }
    public required EOrderStatus  ToStatus { get; set; }
    public string? Note { get; set; }
    
    public virtual Orders? Order { get; set; }
}