namespace UserManagementApi.DTOs;

/// <summary>
/// Response for natural language search with interpretation.
/// </summary>
public record NaturalLanguageSearchResponse
{
    /// <summary>
    /// The products matching the search.
    /// </summary>
    public IEnumerable<ProductResponse> Products { get; init; } = Enumerable.Empty<ProductResponse>();
    
    /// <summary>
    /// How the AI interpreted the query.
    /// </summary>
    public string Interpretation { get; init; } = string.Empty;
    
    /// <summary>
    /// The filters that were applied.
    /// </summary>
    public ProductSearchFilters AppliedFilters { get; init; } = new();
    
    /// <summary>
    /// Number of results found.
    /// </summary>
    public int ResultCount { get; init; }
}