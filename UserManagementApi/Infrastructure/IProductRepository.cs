using UserManagementApi.Domain;

namespace UserManagementApi.Infrastructure;

/// <summary>
/// Repository interface for product data access operations.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves all products.
    /// </summary>
    Task<Result<IEnumerable<Product>, Error>> GetAllAsync();
    
    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    Task<Result<Product, Error>> GetByIdAsync(int id);
    
    /// <summary>
    /// Retrieves a page of products using cursor-based pagination.
    /// </summary>
    /// <param name="afterId">Get products after this ID (null for first page)</param>
    /// <param name="pageSize">Number of products to retrieve (max 100)</param>
    Task<Result<PagedResult<Product>, Error>> GetPagedAsync(int? afterId, int pageSize);
    
}