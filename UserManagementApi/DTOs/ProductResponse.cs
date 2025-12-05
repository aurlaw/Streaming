namespace UserManagementApi.DTOs;

/// <summary>
/// Response model for product data.
/// </summary>
public record ProductResponse(int Id, string Name, decimal Price);