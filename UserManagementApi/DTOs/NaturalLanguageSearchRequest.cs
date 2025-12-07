namespace UserManagementApi.DTOs;

/// <summary>
/// Request for natural language product search.
/// </summary>
public record NaturalLanguageSearchRequest
{
    /// <summary>
    /// Natural language search query (e.g., "affordable gaming laptops under $1000").
    /// </summary>
    public string Query { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of results to return (default 20).
    /// </summary>
    public int PageSize { get; init; } = 20;
}