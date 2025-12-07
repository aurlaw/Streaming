using System.Text;
using Microsoft.Extensions.AI;
using UserManagementApi.Domain;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;

namespace UserManagementApi.Services;

/// <summary>
/// Implementation of product tagging service using AI.
/// </summary>
public class ProductTaggingService : IProductTaggingService
{
    private readonly IChatClient _chatClient;
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductTaggingService> _logger;
    private readonly IPromptService _promptService; 

    public ProductTaggingService(
        IChatClient chatClient,
        IProductRepository repository,
        ILogger<ProductTaggingService> logger, IPromptService promptService)
    {
        _chatClient = chatClient;
        _repository = repository;
        _logger = logger;
        _promptService = promptService;
    }

    public async Task<Result<List<string>, Error>> GenerateTagsAsync(Product product)
    {
        try
        {
            var prompt = BuildTaggingPrompt(product);
            // Load prompt from markdown file
            var systemPrompt = await _promptService.GetPromptAsync("product-search/product-tagging");
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, prompt)
            };

            var options = new ChatOptions
            {
                Temperature = 0.3f, // Lower temperature for more consistent tagging
                MaxOutputTokens = 200
            };

            _logger.LogInformation(
                "Generating tags for product {ProductId}: {ProductName}", 
                product.Id, 
                product.Name);

            var response = await _chatClient.GetResponseAsync(messages, options);
            var content = response.Text ?? string.Empty;

            var parseResult = ParseTagsFromResponse(content);

            if (parseResult is Result<List<string>, Error>.Failure(var error))
            {
                var errorMsg = error switch
                {
                    Error.ValidationError(var msg) => msg,
                    _ => "Unknown error"
                };
                
                _logger.LogError(
                    "Failed to parse tags for product {ProductId}: {Error}", 
                    product.Id, 
                    errorMsg);
            }
            return parseResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error generating tags for product {ProductId}", 
                product.Id);
            return new Result<List<string>, Error>.Failure(
                new Error.ValidationError($"Failed to generate tags: {ex.Message}"));
        }
    }

    public async Task<List<(int ProductId, Result<List<string>, Error> TagsResult)>> 
        GenerateTagsBatchAsync(IEnumerable<Product> products)
    {
        var results = new List<(int ProductId, Result<List<string>, Error> TagsResult)>();
        var productList = products.ToList();

        _logger.LogInformation(
            "Starting batch tag generation for {Count} products", 
            productList.Count);

        // Process in smaller batches to avoid rate limits
        const int batchSize = 10;
        for (int i = 0; i < productList.Count; i += batchSize)
        {
            var batch = productList.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(async product =>
            {
                var result = await GenerateTagsAsync(product);
                return (product.Id, result);
            });

            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);

            // Small delay between batches to be respectful of rate limits
            if (i + batchSize < productList.Count)
            {
                await Task.Delay(100);
            }
        }

        var successCount = results.Count(r => r.TagsResult is Result<List<string>, Error>.Success);
        _logger.LogInformation(
            "Batch tag generation complete: {SuccessCount}/{TotalCount} successful", 
            successCount, 
            results.Count);

        return results;
    }

    public async Task<Result<Product, Error>> UpdateProductTagsAsync(
        int productId, 
        List<string> tags)
    {
        return await _repository.GetByIdAsync(productId)
            .ThenAsync(product => UpdateProductWithTags(product, tags))
            .ThenAsync(updated => _repository.UpdateAsync(updated))
            .LogSuccess(_logger, "Updated tags for product {ProductId}", productId)
            .LogFailure(_logger, "Failed to update tags for product {ProductId}", productId);
    }

    public async Task<Result<int, Error>> TagAllUntaggedProductsAsync(bool ignoreTagged)
    {
        _logger.LogInformation("Starting to tag all untagged products");

        // Get all products without tags
        var allProductsResult = await _repository.GetAllAsync();
        if (allProductsResult is Result<IEnumerable<Product>, Error>.Failure(var error))
        {
            return new Result<int, Error>.Failure(error);
        }

        var allProducts = ((Result<IEnumerable<Product>, Error>.Success)allProductsResult).Value;

        if (!ignoreTagged)
        {
            allProducts = allProducts
                .Where(p => string.IsNullOrWhiteSpace(p.Tags));

        }
        var untaggedProducts = allProducts .ToList();

        if (untaggedProducts.Count == 0)
        {
            _logger.LogInformation("No untagged products found");
            return new Result<int, Error>.Success(0);
        }

        _logger.LogInformation(
            "Found {Count} untagged products. Starting batch processing...", 
            untaggedProducts.Count);

        // Generate tags for all untagged products
        var tagResults = await GenerateTagsBatchAsync(untaggedProducts);

        // Update products with successfully generated tags
        int successCount = 0;
        foreach (var (productId, tagsResult) in tagResults)
        {
            if (tagsResult is Result<List<string>, Error>.Success(var tags))
            {
                var updateResult = await UpdateProductTagsAsync(productId, tags);
                if (updateResult is Result<Product, Error>.Success)
                {
                    successCount++;
                }
            }
        }

        _logger.LogInformation(
            "Tagging complete: {SuccessCount}/{TotalCount} products tagged successfully", 
            successCount, 
            untaggedProducts.Count);

        return new Result<int, Error>.Success(successCount);
    }



    private static string BuildTaggingPrompt(Product product)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate tags for this product:");
        sb.AppendLine($"Name: {product.Name}");
        sb.AppendLine($"Category: {product.Category}");
        sb.AppendLine($"Brand: {product.Brand}");
        sb.AppendLine($"Price: ${product.Price}");
        sb.AppendLine($"Description: {product.Description}");
        
        if (product.Rating.HasValue)
        {
            sb.AppendLine($"Rating: {product.Rating:F1}/5.0");
        }

        return sb.ToString();
    }

    private Result<List<string>, Error> ParseTagsFromResponse(string response)
    {
        try
        {
            // Clean up the response (remove any markdown, extra whitespace, etc.)
            var cleaned = response
                .Replace("```", "")
                .Replace("Tags:", "")
                .Trim();

            // Split by comma and clean each tag
            var tags = cleaned
                .Split(',')
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct()
                .ToList();

            if (tags.Count == 0)
            {
                return new Result<List<string>, Error>.Failure(
                    new Error.ValidationError("No valid tags found in response"));
            }

            return new Result<List<string>, Error>.Success(tags);
        }
        catch (Exception ex)
        {
            return new Result<List<string>, Error>.Failure(
                new Error.ValidationError($"Failed to parse tags: {ex.Message}"));
        }
    }

    private static Task<Result<Product, Error>> UpdateProductWithTags(
        Product product, 
        List<string> tags)
    {
        var tagsString = string.Join(",", tags);
        var updated = product with { Tags = tagsString };
        return Task.FromResult<Result<Product, Error>>(new Result<Product, Error>.Success(updated));
    }
}