namespace UserManagementApi.DTOs;

/// <summary>
/// Response model for paginated products.
/// </summary>
public record PagedProductsResponse(
    IEnumerable<ProductResponse> Products,
    string? NextCursor,
    bool HasMore
);