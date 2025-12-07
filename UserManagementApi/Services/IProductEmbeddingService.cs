using UserManagementApi.Domain;

namespace UserManagementApi.Services;

/// <summary>
/// Service for generating and managing product embeddings.
/// </summary>
public interface IProductEmbeddingService
{
    /// <summary>
    /// Generates embeddings for all products that don't have them.
    /// </summary>
    Task<Result<int, Error>> GenerateEmbeddingsForAllProductsAsync();
    
    /// <summary>
    /// Generates an embedding for a single product.
    /// </summary>
    Task<Result<Product, Error>> GenerateEmbeddingForProductAsync(int productId);
    
    /// <summary>
    /// Regenerates embeddings for all products (even those that already have them).
    /// </summary>
    Task<Result<int, Error>> RegenerateAllEmbeddingsAsync();
}