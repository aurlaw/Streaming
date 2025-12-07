using System.Text.Json;
using Microsoft.Extensions.AI;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Infrastructure;

namespace UserManagementApi.Services;

/// <summary>
/// Service for natural language product search using GPT function calling.
/// </summary>
public class NaturalLanguageSearchService : INaturalLanguageSearchService
{
    private readonly IChatClient _chatClient;
    private readonly IProductRepository _repository;
    private readonly ILogger<NaturalLanguageSearchService> _logger;
    private readonly IPromptService _promptService; 
    
    public NaturalLanguageSearchService(
        IChatClient chatClient,
        IProductRepository repository,
        ILogger<NaturalLanguageSearchService> logger, IPromptService promptService)
    {
        _chatClient = chatClient;
        _repository = repository;
        _logger = logger;
        _promptService = promptService;
    }
    
    public async Task<Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>> 
        SearchAsync(string query, int limit)
    {
        try
        {
            _logger.LogInformation("Processing natural language search: {Query}", query);
            
            // Step 1: Use GPT to extract filters from natural language
            var filtersResult = await ExtractFiltersAsync(query);
            
            if (filtersResult is Result<ProductSearchFilters, Error>.Failure(var error))
            {
                return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(error);
            }
            
            var filters = ((Result<ProductSearchFilters, Error>.Success)filtersResult).Value;
            
            _logger.LogInformation("Extracted filters: {@Filters}", filters);
            
            // Step 2: Search database with extracted filters
            var searchResult = await _repository.SearchWithFiltersAsync(filters, limit);
            
            if (searchResult is Result<IEnumerable<Product>, Error>.Failure(var searchError))
            {
                return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(searchError);
            }
            
            var products = ((Result<IEnumerable<Product>, Error>.Success)searchResult).Value;
            
            // Step 3: Generate human-readable interpretation
            var interpretation = GenerateInterpretation(filters, products.Count());
            
            _logger.LogInformation("Found {Count} products matching query", products.Count());
            
            return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Success(
                (products, filters, interpretation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing natural language search");
            return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(
                new Error.DatabaseError($"Search failed: {ex.Message}"));
        }
    }
    
    private async Task<Result<ProductSearchFilters, Error>> ExtractFiltersAsync(string query)
    {
        try
        {
            // Load prompt from markdown file
            var systemPrompt = await _promptService.GetPromptAsync("product-search/filter-extraction");

            var messages = new[]
            {
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, $"Extract search filters from this query: {query}")
            };
            
            var response = await _chatClient.GetResponseAsync(messages);
            var content = response.Text ?? string.Empty;
            
            // Parse JSON response
            var filters = JsonSerializer.Deserialize<ProductSearchFilters>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (filters == null)
            {
                return new Result<ProductSearchFilters, Error>.Failure(
                    new Error.ValidationError("Failed to extract filters from query"));
            }
            
            return new Result<ProductSearchFilters, Error>.Success(filters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting filters from query");
            return new Result<ProductSearchFilters, Error>.Failure(
                new Error.ValidationError($"Failed to parse query: {ex.Message}"));
        }
    }
    
    private string GenerateInterpretation(ProductSearchFilters filters, int resultCount)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(filters.Category))
            parts.Add($"{filters.Category.ToLower()}s");
        else
            parts.Add("products");
        
        if (!string.IsNullOrWhiteSpace(filters.Brand))
            parts.Add($"from {filters.Brand}");
        
        if (filters.MinPrice.HasValue && filters.MaxPrice.HasValue)
            parts.Add($"between ${filters.MinPrice:N0} and ${filters.MaxPrice:N0}");
        else if (filters.MaxPrice.HasValue)
            parts.Add($"under ${filters.MaxPrice:N0}");
        else if (filters.MinPrice.HasValue)
            parts.Add($"over ${filters.MinPrice:N0}");
        
        if (filters.MinRating.HasValue)
            parts.Add($"with {filters.MinRating}+ star rating");
        
        if (filters.Tags != null && filters.Tags.Any())
            parts.Add($"tagged as {string.Join(", ", filters.Tags)}");
        
        if (filters.InStock == true)
            parts.Add("in stock");
        
        var interpretation = "Showing " + string.Join(" ", parts);
        
        if (resultCount == 0)
            interpretation += ". No products found matching these criteria.";
        else
            interpretation += $". Found {resultCount} {(resultCount == 1 ? "product" : "products")}.";
        
        return interpretation;
    }
}