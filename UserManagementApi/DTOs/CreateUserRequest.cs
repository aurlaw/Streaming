namespace UserManagementApi.DTOs;

/// <summary>
/// Request model for creating a new user.
/// </summary>
public record CreateUserRequest(string Name, string Email);