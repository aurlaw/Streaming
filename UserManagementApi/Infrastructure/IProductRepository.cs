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
}