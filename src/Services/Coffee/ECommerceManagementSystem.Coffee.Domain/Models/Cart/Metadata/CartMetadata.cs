namespace ECommerceManagementSystem.Coffee.Domain.Models.Cart.Metadata;

public class CartMetadata
{
    public int CartCount { get; set; } = 0;
    public Guid? ActiveCartId { get; set; }
    public List<Guid> CartIds { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}