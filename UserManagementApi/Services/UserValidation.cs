using UserManagementApi.Domain;
using UserManagementApi.DTOs;

namespace UserManagementApi.Services;

/// <summary>
/// Validation logic for user operations.
/// </summary>
public static class UserValidation
{
    /// <summary>
    /// Validates a user creation request.
    /// </summary>
    public static Result<CreateUserRequest, Error> ValidateCreateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new Result<CreateUserRequest, Error>.Failure(
                new Error.ValidationError("Name is required"));
        
        if (request.Name.Length > 100)
            return new Result<CreateUserRequest, Error>.Failure(
                new Error.ValidationError("Name must be 100 characters or less"));
        
        if (string.IsNullOrWhiteSpace(request.Email))
            return new Result<CreateUserRequest, Error>.Failure(
                new Error.ValidationError("Email is required"));
        
        if (!IsValidEmail(request.Email))
            return new Result<CreateUserRequest, Error>.Failure(
                new Error.ValidationError("Email format is invalid"));
        
        return new Result<CreateUserRequest, Error>.Success(request);
    }
    
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}