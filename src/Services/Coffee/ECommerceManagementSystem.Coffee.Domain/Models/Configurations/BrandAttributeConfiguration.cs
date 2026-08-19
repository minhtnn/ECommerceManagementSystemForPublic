namespace ECommerceManagementSystem.Coffee.Domain.Models.Configurations;

public class BrandAttributeConfiguration
{
    public string Value { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BrandConfiguration
{
    public Dictionary<string, BrandAttributeConfiguration> Attributes { get; set; } = new();
}