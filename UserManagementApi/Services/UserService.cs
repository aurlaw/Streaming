using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;

namespace UserManagementApi.Services;
/// <summary>
/// Business logic for user operations.
/// </summary>
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    
    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    public async Task<Result<User, Error>> GetUserByIdAsync(Guid id) =>
        await ValidateUserId(id)
            .ToAsync()
            .ThenAsync(_ => _repository.GetByIdAsync(id))
            .LogSuccess(_logger, "Successfully fetched user: {UserId}", id)
            .LogFailure(_logger, "Failed to fetch user {UserId}", id);
    
    /// <summary>
    /// Creates a new user.
    /// </summary>
    public async Task<Result<User, Error>> CreateUserAsync(CreateUserRequest request) =>
        await UserValidation.ValidateCreateRequest(request)
            .ToAsync()
            .ThenAsync(async validRequest =>
            {
                if (await _repository.EmailExistsAsync(validRequest.Email))
                    return new Result<User, Error>.Failure(
                        new Error.DuplicateError($"User with email {validRequest.Email} already exists"));
                
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = validRequest.Name,
                    Email = validRequest.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                return await _repository.CreateAsync(user);
            })
            .LogSuccess(_logger, "Successfully created user with email: {Email}", request.Email)
            .LogFailure(_logger, "Failed to create user with email {Email}", request.Email);
    
    private static Result<Guid, Error> ValidateUserId(Guid id)
    {
        if (id == Guid.Empty)
            return new Result<Guid, Error>.Failure(
                new Error.ValidationError("User ID cannot be empty"));
        
        return new Result<Guid, Error>.Success(id);
    }
}
