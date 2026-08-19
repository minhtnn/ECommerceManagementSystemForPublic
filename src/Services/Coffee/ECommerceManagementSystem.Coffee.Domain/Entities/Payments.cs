using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Payments : EntityAuditBase<Guid>
{
    public Guid OrderId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string? PaymentMethodCodeSnapshot  { get; set; }
    public decimal Amount  { get; set; }
    public EPaymentStatus  PaymentStatus { get; set; }
    public string? TransactionId  { get; set; }
    public string? GateWayResponse {get; set;}
    public DateTime? PaidAt  { get; set; }
    public DateTime? FailedAt   { get; set; }
    public string? FailedReason {get; set;}
    public virtual Orders? Order { get; set; }
    public virtual PaymentMethods? PaymentMethod { get; set; }
}