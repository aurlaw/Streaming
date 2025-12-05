using Microsoft.EntityFrameworkCore;
using UserManagementApi.Domain;
using UserManagementApi.Mappers;

namespace UserManagementApi.Infrastructure;

/// <summary>
/// Repository implementation for user data access operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Result<User, Error>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _context.Users.FindAsync(id);
            
            if (entity == null)
                return new Result<User, Error>.Failure(
                    new Error.NotFoundError($"User with ID {id} not found"));
            
            return new Result<User, Error>.Success(UserMapper.ToDomain(entity));
        }
        catch (Exception ex)
        {
            return new Result<User, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
    
    public async Task<Result<User, Error>> GetByEmailAsync(string email)
    {
        try
        {
            var entity = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            
            if (entity == null)
                return new Result<User, Error>.Failure(
                    new Error.NotFoundError($"User with email {email} not found"));
            
            return new Result<User, Error>.Success(UserMapper.ToDomain(entity));
        }
        catch (Exception ex)
        {
            return new Result<User, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
    
    public async Task<Result<User, Error>> CreateAsync(User user)
    {
        try
        {
            var entity = UserMapper.ToEntity(user);
            await _context.Users.AddAsync(entity);
            await _context.SaveChangesAsync();
            
            return new Result<User, Error>.Success(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") ?? false)
        {
            return new Result<User, Error>.Failure(
                new Error.DuplicateError($"User with email {user.Email} already exists"));
        }
        catch (Exception ex)
        {
            return new Result<User, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}