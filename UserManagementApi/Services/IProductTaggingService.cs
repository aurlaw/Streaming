using UserManagementApi.Domain;

namespace UserManagementApi.Services;

/// <summary>
/// Service for generating and managing product tags using AI.
/// </summary>
public interface IProductTaggingService
{
    /// <summary>
    /// Generates tags for a single product based on its attributes.
    /// </summary>
    Task<Result<List<string>, Error>> GenerateTagsAsync(Product product);
    
    /// <summary>
    /// Generates tags for multiple products in batch.
    /// Returns results for each product (success or failure).
    /// </summary>
    Task<List<(int ProductId, Result<List<string>, Error> TagsResult)>> GenerateTagsBatchAsync(
        IEnumerable<Product> products);
    
    /// <summary>
    /// Updates a product's tags in the database.
    /// </summary>
    Task<Result<Product, Error>> UpdateProductTagsAsync(int productId, List<string> tags);
    
    /// <summary>
    /// Generates and saves tags for all products that don't have tags.
    /// Returns count of successfully tagged products.
    /// </summary>
    Task<Result<int, Error>> TagAllUntaggedProductsAsync(bool ignoreTagged);

    
}