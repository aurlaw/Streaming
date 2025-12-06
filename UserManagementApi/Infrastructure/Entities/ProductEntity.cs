namespace UserManagementApi.Infrastructure.Entities;

/// <summary>
/// Entity Framework entity for persisting products to the database.
/// </summary>
public class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Tags { get; set; } = string.Empty;
}