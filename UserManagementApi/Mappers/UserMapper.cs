using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Infrastructure.Entities;

namespace UserManagementApi.Mappers;

/// <summary>
/// Maps between User domain models, entities, and DTOs.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converts a database entity to a domain model.
    /// </summary>
    public static User ToDomain(UserEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Email = entity.Email,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
    
    /// <summary>
    /// Converts a domain model to a database entity.
    /// </summary>
    public static UserEntity ToEntity(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
    
    /// <summary>
    /// Converts a domain model to an API response DTO.
    /// </summary>
    public static UserResponse ToResponse(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.IsActive
    );
}
