namespace UserManagementApi.Domain;

/// <summary>
/// Represents a user in the domain model.
/// </summary>
public record User
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}