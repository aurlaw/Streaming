using Microsoft.EntityFrameworkCore;
using UserManagementApi.Domain;
using UserManagementApi.Mappers;

namespace UserManagementApi.Infrastructure;

/// <summary>
/// Repository implementation for product data access operations.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    
    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Result<IEnumerable<Product>, Error>> GetAllAsync()
    {
        try
        {
            var entities = await _context.Products.ToListAsync();
            var products = entities.Select(ProductMapper.ToDomain);
            
            return new Result<IEnumerable<Product>, Error>.Success(products);
        }
        catch (Exception ex)
        {
            return new Result<IEnumerable<Product>, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
    
    public async Task<Result<Product, Error>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await _context.Products.FindAsync(id);
            
            if (entity == null)
                return new Result<Product, Error>.Failure(
                    new Error.NotFoundError($"Product with ID {id} not found"));
            
            return new Result<Product, Error>.Success(ProductMapper.ToDomain(entity));
        }
        catch (Exception ex)
        {
            return new Result<Product, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
}