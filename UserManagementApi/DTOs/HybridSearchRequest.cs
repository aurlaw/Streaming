namespace UserManagementApi.DTOs;

/// <summary>
/// Request for hybrid search (structured filters + semantic ranking).
/// </summary>
public record HybridSearchRequest
{
    /// <summary>
    /// Natural language search query.
    /// </summary>
    public string Query { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of results to return (default 20).
    /// </summary>
    public int PageSize { get; init; } = 20;
}