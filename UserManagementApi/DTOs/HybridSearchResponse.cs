namespace UserManagementApi.DTOs;

/// <summary>
/// Response for hybrid search with interpretation and relevance scores.
/// </summary>
public record HybridSearchResponse
{
    /// <summary>
    /// Products ranked by relevance.
    /// </summary>
    public IEnumerable<ProductResponse> Products { get; init; } = Enumerable.Empty<ProductResponse>();
    
    /// <summary>
    /// How the AI interpreted the query.
    /// </summary>
    public string Interpretation { get; init; } = string.Empty;
    
    /// <summary>
    /// The structured filters that were applied.
    /// </summary>
    public ProductSearchFilters AppliedFilters { get; init; } = new();
    
    /// <summary>
    /// Number of results found.
    /// </summary>
    public int ResultCount { get; init; }
    
    /// <summary>
    /// Search mode used (hybrid = structured + semantic).
    /// </summary>
    public string SearchMode { get; init; } = "hybrid";
}