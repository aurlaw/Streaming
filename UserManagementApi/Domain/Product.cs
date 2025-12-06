using Pgvector;

namespace UserManagementApi.Domain;

/// <summary>
/// Represents a product in the domain model.
/// </summary>
public record Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    public decimal? Rating { get; init; }
    public int ReviewCount { get; init; }
    public string Tags { get; init; } = string.Empty;
    public Vector? Embedding { get; init; }
}
