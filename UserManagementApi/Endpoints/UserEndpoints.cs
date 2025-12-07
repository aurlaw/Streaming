using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;
using UserManagementApi.Mappers;
using UserManagementApi.Services;

namespace UserManagementApi.Endpoints;

/// <summary>
/// Endpoints for user management operations.
/// </summary>
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{encodedId}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get a user by ID")
            .Produces<UserResponse>(200)
            .Produces(400)
            .Produces(404);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .Produces<UserResponse>(201)
            .Produces(400)
            .Produces(409);

        return group;
    }

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    private static async Task<IResult> GetUserById(string encodedId, UserService userService, IIdEncoder encoder)
    {
        var result = await userService.GetUserByEncodedIdAsync(encodedId)
            .MapAsync(user => UserMapper.ToResponse(user, encoder));
        
        return result switch
        {
            Result<UserResponse, Error>.Success(var response) => 
                Results.Ok(response),
            Result<UserResponse, Error>.Failure(Error.ValidationError(var msg)) =>
                Results.BadRequest(new { error = msg }),
            Result<UserResponse, Error>.Failure(Error.NotFoundError(var msg)) => 
                Results.NotFound(new { error = msg }),
            Result<UserResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    private static async Task<IResult> CreateUser(CreateUserRequest request, UserService userService, IIdEncoder encoder)
    {
        var result = await userService.CreateUserAsync(request)
            .MapAsync(user => UserMapper.ToResponse(user, encoder));
        
        return result switch
        {
            Result<UserResponse, Error>.Success(var response) => 
                Results.Created($"/api/users/{response.Id}", response),
            Result<UserResponse, Error>.Failure(Error.ValidationError(var msg)) => 
                Results.BadRequest(new { error = msg }),
            Result<UserResponse, Error>.Failure(Error.DuplicateError(var msg)) => 
                Results.Conflict(new { error = msg }),
            Result<UserResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };
    }
}