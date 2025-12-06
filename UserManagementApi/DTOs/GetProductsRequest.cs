namespace UserManagementApi.DTOs;

/// <summary>
/// Request model for paginated product retrieval.
/// </summary>
public record GetProductsRequest
{
    /// <summary>
    /// Number of products to return per page.
    /// </summary>
    public int PageSize { get; init; } = 20;
    
    /// <summary>
    /// Cursor for pagination (encoded product ID to start after).
    /// Null for the first page.
    /// </summary>
    public string? Cursor { get; init; }
    
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;
}