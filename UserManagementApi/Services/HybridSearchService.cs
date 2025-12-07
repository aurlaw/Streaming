using System.Text.Json;
using Microsoft.Extensions.AI;
using Pgvector;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Infrastructure;

namespace UserManagementApi.Services;

/// <summary>
/// Service for hybrid search using GPT function calling + vector similarity.
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly IChatClient _chatClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IProductRepository _repository;
    private readonly ILogger<HybridSearchService> _logger;
    private readonly IPromptService _promptService;
    
    public HybridSearchService(
        IChatClient chatClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IProductRepository repository,
        ILogger<HybridSearchService> logger, IPromptService promptService)
    {
        _chatClient = chatClient;
        _embeddingGenerator = embeddingGenerator;
        _repository = repository;
        _logger = logger;
        _promptService = promptService;
    }
    
    public async Task<Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>> 
        SearchAsync(string query, int limit)
    {
        try
        {
            _logger.LogInformation("Processing hybrid search: {Query}", query);
            
            // Step 1: Extract structured filters using GPT
            var filtersResult = await ExtractFiltersAsync(query);
            
            if (filtersResult is Result<ProductSearchFilters, Error>.Failure(var filterError))
            {
                return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(filterError);
            }
            
            var filters = ((Result<ProductSearchFilters, Error>.Success)filtersResult).Value;
            
            _logger.LogInformation("Extracted filters: {@Filters}", filters);
            
            // Step 2: Generate embedding for semantic part of query
            var embeddingResult = await GenerateQueryEmbeddingAsync(query);
            
            if (embeddingResult is Result<Vector, Error>.Failure(var embeddingError))
            {
                return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(embeddingError);
            }
            
            var queryEmbedding = ((Result<Vector, Error>.Success)embeddingResult).Value;
            
            _logger.LogInformation("Generated query embedding");
            
            // Step 3: Hybrid search - structured filters + semantic ranking
            var searchResult = await _repository.HybridSearchAsync(filters, queryEmbedding, limit);
            
            if (searchResult is Result<IEnumerable<Product>, Error>.Failure(var searchError))
            {
                return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(searchError);
            }
            
            var products = ((Result<IEnumerable<Product>, Error>.Success)searchResult).Value;
            
            // Step 4: Generate human-readable interpretation
            var interpretation = GenerateInterpretation(filters, products.Count());
            
            _logger.LogInformation("Found {Count} products matching hybrid search", products.Count());
            
            return new Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Success(
                (products, filters, interpretation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing hybrid search");
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
            
            _logger.LogDebug("Raw GPT response: {Response}", content);
            
            // Strip markdown code fences if present
            content = content.Trim();
            if (content.StartsWith("```"))
            {
                var firstNewline = content.IndexOf('\n');
                if (firstNewline > 0)
                {
                    content = content.Substring(firstNewline + 1);
                }
                
                if (content.EndsWith("```"))
                {
                    content = content.Substring(0, content.Length - 3);
                }
                
                content = content.Trim();
            }
            
            _logger.LogDebug("Cleaned JSON: {Json}", content);
            
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
    
    private async Task<Result<Vector, Error>> GenerateQueryEmbeddingAsync(string query)
    {
        try
        {
            var embeddings = await _embeddingGenerator.GenerateAsync([query]);
            var embedding = embeddings.FirstOrDefault();
            
            if (embedding == null)
            {
                return new Result<Vector, Error>.Failure(
                    new Error.ValidationError("Failed to generate query embedding"));
            }
            
            var vector = new Vector(embedding.Vector.ToArray());
            return new Result<Vector, Error>.Success(vector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating query embedding");
            return new Result<Vector, Error>.Failure(
                new Error.ValidationError($"Failed to generate embedding: {ex.Message}"));
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
        
        var interpretation = "Showing " + string.Join(" ", parts) + ", ranked by relevance";
        
        if (resultCount == 0)
            interpretation += ". No products found matching these criteria.";
        else
            interpretation += $". Found {resultCount} {(resultCount == 1 ? "product" : "products")}.";
        
        return interpretation;
    }
}