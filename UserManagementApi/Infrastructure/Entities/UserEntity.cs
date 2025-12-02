namespace UserManagementApi.Infrastructure.Entities;

/// <summary>
/// Entity Framework entity for persisting users to the database.
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}