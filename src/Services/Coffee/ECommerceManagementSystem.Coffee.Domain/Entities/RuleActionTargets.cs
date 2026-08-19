using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class RuleActionTargets : EntityAuditBase<Guid>
{
    public required Guid RuleActionId { get; set; }
    public EActionTargetType TargetType {get; set;}
    public Guid TargetId {get; set;}
    public int Quantity {get; set;}
    public EActionTargetRole Role {get; set;}
    
    public virtual RuleActions? RuleAction { get; set; }
}