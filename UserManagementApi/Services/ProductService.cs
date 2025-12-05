using UserManagementApi.Domain;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;

namespace UserManagementApi.Services;

/// <summary>
/// Business logic for product operations.
/// </summary>
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    /// <summary>
    /// Retrieves all products.
    /// </summary>
    public async Task<Result<IEnumerable<Product>, Error>> GetAllProductsAsync() =>
        await _repository.GetAllAsync()
            .LogSuccess(_logger, "Successfully fetched all products")
            .LogFailure(_logger, "Failed to fetch all products");
    
    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    public async Task<Result<Product, Error>> GetProductByIdAsync(int id) =>
        await ValidateProductId(id)
            .ToAsync()
            .ThenAsync(_ => _repository.GetByIdAsync(id))
            .LogSuccess(_logger, "Successfully fetched product: {ProductId}", id)
            .LogFailure(_logger, "Failed to fetch product {ProductId}", id);
    
    private static Result<int, Error> ValidateProductId(int id)
    {
        if (id <= 0)
            return new Result<int, Error>.Failure(
                new Error.ValidationError("Product ID must be greater than zero"));
        
        return new Result<int, Error>.Success(id);
    }
}