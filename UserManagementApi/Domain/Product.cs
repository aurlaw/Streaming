namespace UserManagementApi.Domain;

/// <summary>
/// Represents a product in the domain model.
/// </summary>
public record Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
