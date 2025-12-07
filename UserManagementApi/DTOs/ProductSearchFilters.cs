namespace UserManagementApi.DTOs;

/// <summary>
/// Structured filters extracted from natural language query.
/// </summary>
public record ProductSearchFilters
{
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public decimal? MinRating { get; init; }
    public List<string>? Tags { get; init; }
    public bool? InStock { get; init; }
}