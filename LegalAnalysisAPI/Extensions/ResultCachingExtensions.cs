using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using LegalAnalysisAPI.Domain;
using Microsoft.Extensions.Caching.Distributed;

namespace UserManagementApi.Extensions;

/// <summary>
/// Extension methods for adding caching to Result pipelines.
/// </summary>
public static class ResultCachingExtensions
{
    /// <summary>
    /// Attempts to retrieve the result from cache. If not found, executes the operation
    /// and caches successful results.
    /// </summary>
    public static async Task<Result<T, E>> WithCache<T, E>(
        this Task<Result<T, E>> resultTask,
        IDistributedCache cache,
        string cacheKey,
        TimeSpan expiration,
        JsonTypeInfo<T> jsonTypeInfo)
    {
        // Try to get from cache
        var cached = await cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            var cachedValue = JsonSerializer.Deserialize(cached, jsonTypeInfo);
            if (cachedValue != null)
                return new Result<T, E>.Success(cachedValue);
        }
        
        // Execute the operation
        var result = await resultTask;
        
        // Cache successful results
        if (result is Result<T, E>.Success(var value))
        {
            var serialized = JsonSerializer.Serialize(value, jsonTypeInfo);
            await cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }
        
        return result;
    }
}