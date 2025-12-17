using System.Text.Json.Serialization;

namespace VoiceEssay.Infrastructure;

/// <summary>
/// JSON serialization context for AOT compatibility.
/// </summary>

[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}