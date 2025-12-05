namespace UserManagementApi.Infrastructure.Entities;

/// <summary>
/// Entity Framework entity for persisting products to the database.
/// </summary>
public class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}