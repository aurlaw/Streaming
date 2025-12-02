namespace UserManagementApi.DTOs;
/// <summary>
/// Response model for user data.
/// </summary>
public record UserResponse(Guid Id, string Name, string Email, bool IsActive);
