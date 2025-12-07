using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using UserManagementApi.Domain;
using UserManagementApi.Infrastructure;
using UserManagementApi.Infrastructure.Entities;

namespace UserManagementApi.Services;

/// <summary>
/// Service for generating and managing product embeddings using OpenAI.
/// </summary>
public class ProductEmbeddingService : IProductEmbeddingService
{
    private readonly IProductRepository _repository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<ProductEmbeddingService> _logger;
    private readonly AppDbContext _context;
    
    public ProductEmbeddingService(
        IProductRepository repository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<ProductEmbeddingService> logger,
        AppDbContext context)
    {
        _repository = repository;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
        _context = context;
    }
    
    /// <summary>
    /// Generates embeddings for all products that don't have them.
    /// </summary>
    public async Task<Result<int, Error>> GenerateEmbeddingsForAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("Starting embedding generation for products without embeddings");
            
            // Get all products without embeddings
            var productsWithoutEmbeddings = await _context.Products
                .Where(p => p.Embedding == null)
                .ToListAsync();
            
            if (productsWithoutEmbeddings.Count == 0)
            {
                _logger.LogInformation("No products need embeddings");
                return new Result<int, Error>.Success(0);
            }
            
            _logger.LogInformation("Found {Count} products without embeddings", productsWithoutEmbeddings.Count);
            
            var successCount = 0;
            var batchSize = 100; // Process in batches to avoid overwhelming the API
            
            for (int i = 0; i < productsWithoutEmbeddings.Count; i += batchSize)
            {
                var batch = productsWithoutEmbeddings.Skip(i).Take(batchSize).ToList();
                _logger.LogInformation("Processing batch {BatchNum}/{TotalBatches}", 
                    (i / batchSize) + 1, 
                    (productsWithoutEmbeddings.Count + batchSize - 1) / batchSize);
                
                // Generate embeddings for batch
                var texts = batch.Select(p => CreateEmbeddingText(p)).ToList();
                
                try
                {
                    var embeddings = await _embeddingGenerator.GenerateAsync(texts);
                    var embeddingsList = embeddings.ToList();
                    
                    // Update products with embeddings
                    for (int j = 0; j < batch.Count; j++)
                    {
                        var product = batch[j];
                        var embedding = embeddingsList[j];
                        
                        product.Embedding = new Vector(embedding.Vector.ToArray());
                        successCount++;
                    }
                    
                    // Save batch
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Saved embeddings for batch ({SuccessCount} total so far)", successCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embeddings for batch starting at index {Index}", i);
                    // Continue with next batch instead of failing completely
                }
                
                // Small delay to respect rate limits
                if (i + batchSize < productsWithoutEmbeddings.Count)
                {
                    await Task.Delay(100);
                }
            }
            
            _logger.LogInformation("Completed embedding generation. Successfully processed {Count} products", successCount);
            
            return new Result<int, Error>.Success(successCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for products");
            return new Result<int, Error>.Failure(
                new Error.DatabaseError($"Failed to generate embeddings: {ex.Message}"));
        }
    }
    
    /// <summary>
    /// Generates an embedding for a single product.
    /// </summary>
    public async Task<Result<Product, Error>> GenerateEmbeddingForProductAsync(int productId)
    {
        try
        {
            var productResult = await _repository.GetByIdAsync(productId);
            
            if (productResult is Result<Product, Error>.Failure(var error))
                return new Result<Product, Error>.Failure(error);
            
            var product = ((Result<Product, Error>.Success)productResult).Value;
            
            // Generate embedding text
            var text = CreateEmbeddingText(product);
            
            // Generate embedding
            var embeddings = await _embeddingGenerator.GenerateAsync([text]);
            var embedding = embeddings.FirstOrDefault();
            
            if (embedding == null)
            {
                return new Result<Product, Error>.Failure(
                    new Error.ValidationError("Failed to generate embedding"));
            }
            
            // Update product entity in database
            var productEntity = await _context.Products.FindAsync(productId);
            if (productEntity == null)
            {
                return new Result<Product, Error>.Failure(
                    new Error.NotFoundError($"Product with ID {productId} not found"));
            }
            
            productEntity.Embedding = new Vector(embedding.Vector.ToArray());
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Generated embedding for product {ProductId}", productId);
            
            // Return updated product
            var updatedProduct = product with { Embedding = productEntity.Embedding };
            return new Result<Product, Error>.Success(updatedProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for product {ProductId}", productId);
            return new Result<Product, Error>.Failure(
                new Error.DatabaseError($"Failed to generate embedding: {ex.Message}"));
        }
    }
    
    /// <summary>
    /// Regenerates embeddings for all products (even those that already have them).
    /// </summary>
    public async Task<Result<int, Error>> RegenerateAllEmbeddingsAsync()
    {
        try
        {
            _logger.LogInformation("Starting regeneration of all product embeddings");
            
            // Clear all existing embeddings
            await _context.Products
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.Embedding, (Vector?)null));
            
            _logger.LogInformation("Cleared all existing embeddings");
            
            // Generate embeddings for all products
            return await GenerateEmbeddingsForAllProductsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating embeddings");
            return new Result<int, Error>.Failure(
                new Error.DatabaseError($"Failed to regenerate embeddings: {ex.Message}"));
        }
    }
    
    /// <summary>
    /// Creates the text to be embedded from a product.
    /// Combines name, description, category, brand, and tags.
    /// </summary>
    private string CreateEmbeddingText(ProductEntity product)
    {
        // Combine key fields that capture product meaning
        // Order: name, category, brand, description, tags
        // This creates rich context for semantic search
        
        var parts = new List<string>
        {
            product.Name,
            $"Category: {product.Category}",
            $"Brand: {product.Brand}",
            product.Description
        };
        
        if (!string.IsNullOrWhiteSpace(product.Tags))
        {
            parts.Add($"Tags: {product.Tags.Replace(",", ", ")}");
        }
        
        return string.Join(" | ", parts);
    }
    
    /// <summary>
    /// Creates embedding text from domain Product model.
    /// </summary>
    private string CreateEmbeddingText(Product product)
    {
        var parts = new List<string>
        {
            product.Name,
            $"Category: {product.Category}",
            $"Brand: {product.Brand}",
            product.Description
        };
        
        if (!string.IsNullOrWhiteSpace(product.Tags))
        {
            parts.Add($"Tags: {product.Tags.Replace(",", ", ")}");
        }
        
        return string.Join(" | ", parts);
    }
}