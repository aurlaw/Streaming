using Microsoft.Extensions.Caching.Distributed;

namespace VoiceEssay.Services;

/// <summary>
/// Service for loading AI prompts from markdown files with caching.
/// </summary>
public class PromptService : IPromptService
{
    private readonly string _promptsPath;
    private readonly ILogger<PromptService> _logger;
    private readonly bool _enableCaching;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheExpiration;
    
    
    public PromptService(IWebHostEnvironment env, IConfiguration config, ILogger<PromptService> logger, IDistributedCache cache)
    {
        _promptsPath = Path.Combine(env.ContentRootPath, "Prompts");
        _logger = logger;
        _cache = cache;
        _enableCaching = config.GetValue("PromptService:EnableCaching", true);
        _cacheExpiration = TimeSpan.FromHours(config.GetValue("PromptService:CacheExpirationHours", 24));
        
        if (!Directory.Exists(_promptsPath))
        {
            throw new DirectoryNotFoundException(
                $"Prompts directory not found at: {_promptsPath}. Please create the Prompts folder in your project root.");
        }
        
        _logger.LogInformation("PromptService initialized. Prompts path: {Path}, Caching: {Caching}", 
            _promptsPath, _enableCaching);
    }
    
   public async Task<string> GetPromptAsync(string promptPath)
    {
        var cacheKey = $"prompt:{promptPath}";
        
        // Check cache first
        if (_enableCaching)
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Prompt cache hit: {PromptPath}", promptPath);
                return cached;
            }
        }
        
        // Load from file
        var filePath = Path.Combine(_promptsPath, $"{promptPath}.md");
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("Prompt file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Prompt file not found: {promptPath}.md");
        }
        
        _logger.LogDebug("Loading prompt from file: {FilePath}", filePath);
        var content = await File.ReadAllTextAsync(filePath);
        
        // Cache it with expiration
        if (_enableCaching)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(cacheKey, content, options);
            _logger.LogDebug("Cached prompt: {PromptPath} for {Expiration}", promptPath, _cacheExpiration);
        }
        
        return content;
    }
    
    public string GetPrompt(string promptPath)
    {
        // Synchronous version - useful when you can't use async
        return GetPromptAsync(promptPath).GetAwaiter().GetResult();
    }
    
    public Task ClearCacheAsync()
    {
        // Unfortunately IDistributedCache doesn't have a "clear all" method
        // You'd need to track keys or use a cache implementation that supports it
        // For now, we can provide a method to clear specific prompts
        _logger.LogWarning("Distributed cache doesn't support clearing all entries. Cache will expire naturally after {Expiration}", _cacheExpiration);
        return Task.CompletedTask;
    }
    
    public async Task ClearPromptCacheAsync(string promptPath)
    {
        var cacheKey = $"prompt:{promptPath}";
        await _cache.RemoveAsync(cacheKey);
        _logger.LogInformation("Cleared cache for prompt: {PromptPath}", promptPath);
    }
    
    public void ClearCache()
    {
        // Sync version for backward compatibility
        ClearCacheAsync().GetAwaiter().GetResult();
    }
}