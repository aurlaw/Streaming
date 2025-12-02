using System.Text.Json.Serialization;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;

namespace UserManagementApi.Infrastructure;

/// <summary>
/// JSON serialization context for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}