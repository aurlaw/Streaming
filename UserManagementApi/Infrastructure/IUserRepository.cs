using UserManagementApi.Domain;

namespace UserManagementApi.Infrastructure;
/// <summary>
/// Repository interface for user data access operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    Task<Result<User, Error>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    Task<Result<User, Error>> GetByEmailAsync(string email);
    
    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    Task<Result<User, Error>> CreateAsync(User user);
    
    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);
}