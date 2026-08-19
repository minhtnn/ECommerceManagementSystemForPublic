namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum ERole
{
    SystemAdmin,
    BrandAdmin,
    EndCustomer
}

public enum EPolicy
{
    BrandPolicy,
    SystemPolicy,
    SystemOrBrandPolicy,
    BrandOrEndCustomerPolicy,
    EndCustomerPolicy,
}