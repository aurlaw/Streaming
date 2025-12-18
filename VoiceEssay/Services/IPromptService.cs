namespace VoiceEssay.Services;

/// <summary>
/// Service for loading and managing AI prompts from markdown files.
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Gets a prompt by its path (e.g., "product-search/filter-extraction").
    /// </summary>
    Task<string> GetPromptAsync(string promptPath);
    
    /// <summary>
    /// Gets a prompt synchronously (uses cached version if available).
    /// </summary>
    string GetPrompt(string promptPath);
    
    /// <summary>
    /// Clears the cache for a specific prompt.
    /// </summary>
    Task ClearPromptCacheAsync(string promptPath);
    
    /// <summary>
    /// Clears all prompt caches (note: distributed cache may not support this).
    /// </summary>
    Task ClearCacheAsync();
    
    /// <summary>
    /// Clears the prompt cache synchronously.
    /// </summary>
    void ClearCache();
}