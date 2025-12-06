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
    
    public async Task<Result<PagedResult<Product>, Error>> GetPagedAsync(int? afterId, int pageSize)
    {
        try
        {
            // Fetch pageSize + 1 to determine if there are more results
            var query = _context.Products
                .OrderBy(p => p.Id)
                .AsQueryable();
            
            if (afterId.HasValue)
            {
                query = query.Where(p => p.Id > afterId.Value);
            }
            
            var entities = await query
                .Take(pageSize + 1)
                .ToListAsync();
            
            var hasMore = entities.Count > pageSize;
            var items = entities.Take(pageSize).Select(ProductMapper.ToDomain).ToList();
            var nextId = hasMore ? entities[pageSize - 1].Id : (int?)null;
            
            var pagedResult = new PagedResult<Product>
            {
                Items = items,
                NextId = nextId,
                HasMore = hasMore
            };
            
            return new Result<PagedResult<Product>, Error>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return new Result<PagedResult<Product>, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
    
}