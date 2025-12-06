namespace UserManagementApi.DTOs;

/// <summary>
/// Response model for product data.
/// </summary>
public record ProductResponse(
    string Id,
    string Name,
    string Description,
    string Category,
    string Brand,
    decimal Price,
    int Stock,
    bool IsActive,
    decimal? Rating,
    int ReviewCount,
    string Tags
);