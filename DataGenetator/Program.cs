using System;
using System.IO;
using System.Text;
using DataGenetator;
using Utility;

Console.WriteLine("=== Data Generator ===\n");
Console.WriteLine("1. Generate File");
Console.WriteLine("2. Get User");
Console.WriteLine("3. Exit");
Console.Write("\nSelect an option: ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        GenerateFile();
        break;
    case "2":
        GetUser();
        break;
    case "3":
        return;
    default:
        Console.WriteLine("Invalid option");
        break;}


Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();



static void GenerateFile()
{
    var generate = new GenerateFile();
    generate.Run();
}

static void GetUser()
{
    var inputs = new[]
    {
        "11111111-1111-1111-1111-111111111111", // Valid active user
        "22222222-2222-2222-2222-222222222222", // Valid but inactive
        "99999999-9999-9999-9999-999999999999", // Not found
        "invalid-guid",                          // Parse error
        ""                                       // Empty
    };
    
    foreach (var input in inputs)
    {
        Console.WriteLine($"Processing: '{input}'");
        
        // The beautiful part - chain operations, errors propagate automatically
        var result = ParseUserId(input)
            .Then(id => FetchUser(id))
            .Then(user => ValidateUser(user))
            .Map(user => user.Name);
        
        // Handle the result
        var message = result switch
        {
            Result<string, Error>.Success(var name) => 
                $"✓ Success: Welcome {name}!",
            Result<string, Error>.Failure(Error.ValidationError(var msg)) => 
                $"✗ Validation failed: {msg}",
            Result<string, Error>.Failure(Error.NotFoundError(var msg)) => 
                $"✗ Not found: {msg}",
            Result<string, Error>.Failure(Error.DatabaseError(var msg)) => 
                $"✗ Database error: {msg}",
            _ => "✗ Unknown error"
        };
        
        Console.WriteLine(message);
    }    
    
}


static Result<Guid, Error> ParseUserId(string input)
{
    if (string.IsNullOrWhiteSpace(input))
        return new Result<Guid, Error>.Failure(
            new Error.ValidationError("User ID cannot be empty"));
    
    if (!Guid.TryParse(input, out var id))
        return new Result<Guid, Error>.Failure(
            new Error.ValidationError($"'{input}' is not a valid user ID"));
    
    return new Result<Guid, Error>.Success(id);
}

static Result<User, Error> FetchUser(Guid userId)
{
    // Simulate database lookup
    var users = new Dictionary<Guid, User>
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = 
            new User(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "John Doe",
                "john@example.com",
                true),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = 
            new User(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Jane Smith",
                "jane@example.com",
                false)
    };
    
    if (users.TryGetValue(userId, out var user))
        return new Result<User, Error>.Success(user);
    
    return new Result<User, Error>.Failure(
        new Error.NotFoundError($"User with ID {userId} not found"));
}

static Result<User, Error> ValidateUser(User user)
{
    if (!user.IsActive)
        return new Result<User, Error>.Failure(
            new Error.ValidationError($"User '{user.Name}' is not active"));
    
    if (string.IsNullOrWhiteSpace(user.Email))
        return new Result<User, Error>.Failure(
            new Error.ValidationError($"User '{user.Name}' has no email"));
    
    return new Result<User, Error>.Success(user);
}
