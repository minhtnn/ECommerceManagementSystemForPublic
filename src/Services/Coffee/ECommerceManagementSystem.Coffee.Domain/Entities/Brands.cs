using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Brands : EntityAuditBase<Guid>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Fullname { get; set; }
    public string? Slogan { get; set; }
    public string? Email  { get; set; }
    public string? Address   { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LogoUrl  { get; set; }
    public EBrandStatus Status { get; set; }
    public string? Configuration { get; set; }

    public virtual List<ProductCategories>? ProductCategories { get; set; } = new List<ProductCategories>();
    public virtual List<Customers>? Customers { get; set; } = new List<Customers>();
    public virtual List<PromotionRules> PromotionRules { get; set; } = new List<PromotionRules>();
    public virtual List<Posts> Posts { get; set; } = new List<Posts>();
    public virtual List<BrandAccounts> BrandAccounts { get; set; } = new List<BrandAccounts>();
    public virtual List<BrandPaymentMethods> BrandPaymentMethods { get; set; } = new List<BrandPaymentMethods>();
    
}