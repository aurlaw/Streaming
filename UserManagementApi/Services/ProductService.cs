using UserManagementApi.Domain;
using UserManagementApi.DTOs;
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
    private readonly IIdEncoder _encoder;
    
    public ProductService(IProductRepository repository, ILogger<ProductService> logger, IIdEncoder encoder)
    {
        _repository = repository;
        _logger = logger;
        _encoder = encoder;
    }
    
    /// <summary>
    /// Retrieves all products.
    /// </summary>
    public async Task<Result<IEnumerable<Product>, Error>> GetAllProductsAsync() =>
        await _repository.GetAllAsync()
            .LogSuccess(_logger, "Successfully fetched all products")
            .LogFailure(_logger, "Failed to fetch all products");
    
    /// <summary>
    /// Retrieves a paginated list of products.
    /// </summary>
    public async Task<Result<PagedResult<Product>, Error>> GetPagedProductsAsync(GetProductsRequest request) =>
        await ValidatePageSize(request.PageSize)
            .Then(_ => DecodeCursor(request.Cursor))
            .ToAsync()
            .ThenAsync(afterId => _repository.GetPagedAsync(afterId, request.PageSize))
            .LogSuccess(_logger, "Successfully fetched paged products")
            .LogFailure(_logger, "Failed to fetch paged products");    
    
    /// <summary>
    /// Retrieves a product by its encoded ID.
    /// </summary>
    public async Task<Result<Product, Error>> GetProductByEncodedIdAsync(string encodedId) =>
        await _encoder.DecodeInt(encodedId, EntityIds.Product.Prefix)
            .ToAsync()
            .ThenAsync(id => GetProductByIdAsync(id));
    
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
    private static Result<int, Error> ValidatePageSize(int pageSize)
    {
        if (pageSize < GetProductsRequest.MinPageSize)
            return new Result<int, Error>.Failure(
                new Error.ValidationError($"Page size must be at least {GetProductsRequest.MinPageSize}"));
        
        if (pageSize > GetProductsRequest.MaxPageSize)
            return new Result<int, Error>.Failure(
                new Error.ValidationError($"Page size cannot exceed {GetProductsRequest.MaxPageSize}"));
        
        return new Result<int, Error>.Success(pageSize);
    }
    
    private Result<int?, Error> DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return new Result<int?, Error>.Success(null);
        
        var decodeResult = _encoder.DecodeInt(cursor, EntityIds.Product.Prefix);
        
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return decodeResult switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<int, Error>.Success(var id) => new Result<int?, Error>.Success(id),
            Result<int, Error>.Failure(var error) => new Result<int?, Error>.Failure(error)
        };
    }    
}