using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using UserManagementApi.Domain;
using UserManagementApi.DTOs;
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

    public async Task<Result<IEnumerable<Product>, Error>> SearchWithFiltersAsync(
        ProductSearchFilters filters, 
        int limit)
    {
        try
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();
            
            // Apply category filter
            if (!string.IsNullOrWhiteSpace(filters.Category))
            {
                query = query.Where(p => p.Category == filters.Category);
            }
            
            // Apply brand filter
            if (!string.IsNullOrWhiteSpace(filters.Brand))
            {
                query = query.Where(p => p.Brand == filters.Brand);
            }
            
            // Apply price filters
            if (filters.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filters.MinPrice.Value);
            }
            
            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filters.MaxPrice.Value);
            }
            
            // Apply rating filter
            if (filters.MinRating.HasValue)
            {
                query = query.Where(p => p.Rating >= filters.MinRating.Value);
            }
            
            // Apply stock filter
            if (filters.InStock.HasValue && filters.InStock.Value)
            {
                query = query.Where(p => p.Stock > 0);
            }
            
            // Apply tag filters (any tag matches)
            if (filters.Tags != null && filters.Tags.Any())
            {
                query = query.Where(p => filters.Tags.Any(tag => p.Tags.Contains(tag)));
            }
            
            // Order by rating (best first), then by price (lowest first)
            query = query
                .OrderByDescending(p => p.Rating)
                .ThenBy(p => p.Price);
            
            var entities = await query
                .Take(limit)
                .ToListAsync();
            
            var products = entities.Select(ProductMapper.ToDomain);
            
            return new Result<IEnumerable<Product>, Error>.Success(products);
        }
        catch (Exception ex)
        {
            return new Result<IEnumerable<Product>, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }

    public async Task<Result<IEnumerable<Product>, Error>> HybridSearchAsync(
        ProductSearchFilters filters, 
        Vector queryEmbedding,
        int limit)
    {
        try
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .Where(p => p.Embedding != null) // Only products with embeddings
                .AsQueryable();
            
            // Apply structured filters (same as SearchWithFiltersAsync)
            if (!string.IsNullOrWhiteSpace(filters.Category))
            {
                query = query.Where(p => p.Category == filters.Category);
            }
            
            if (!string.IsNullOrWhiteSpace(filters.Brand))
            {
                query = query.Where(p => p.Brand == filters.Brand);
            }
            
            if (filters.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filters.MinPrice.Value);
            }
            
            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filters.MaxPrice.Value);
            }
            
            if (filters.MinRating.HasValue)
            {
                query = query.Where(p => p.Rating >= filters.MinRating.Value);
            }
            
            if (filters.InStock.HasValue && filters.InStock.Value)
            {
                query = query.Where(p => p.Stock > 0);
            }
            
            // Apply tag filters (any tag matches)
            if (filters.Tags != null && filters.Tags.Any())
            {
                query = query.Where(p => filters.Tags.Any(tag => p.Tags.Contains(tag)));
            }
            
            // Apply semantic ranking using vector similarity (cosine distance)
            // Lower distance = more similar = better match
            query = query.OrderBy(p => p.Embedding!.CosineDistance(queryEmbedding));
            
            var entities = await query
                .Take(limit)
                .ToListAsync();
            
            var products = entities.Select(ProductMapper.ToDomain);
            
            return new Result<IEnumerable<Product>, Error>.Success(products);
        }
        catch (Exception ex)
        {
            return new Result<IEnumerable<Product>, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }

    public async Task<Result<Product, Error>> UpdateAsync(Product product)
    {
        try
        {
            var entity = await _context.Products.FindAsync(product.Id);
        
            if (entity == null)
                return new Result<Product, Error>.Failure(
                    new Error.NotFoundError($"Product with ID {product.Id} not found"));
        
            // Update entity with product values
            entity.Name = product.Name;
            entity.Description = product.Description;
            entity.Category = product.Category;
            entity.Brand = product.Brand;
            entity.Price = product.Price;
            entity.Stock = product.Stock;
            entity.IsActive = product.IsActive;
            entity.Rating = product.Rating;
            entity.ReviewCount = product.ReviewCount;
            entity.Tags = product.Tags;
            entity.Embedding = product.Embedding;
        
            await _context.SaveChangesAsync();
        
            return new Result<Product, Error>.Success(ProductMapper.ToDomain(entity));
        }
        catch (Exception ex)
        {
            return new Result<Product, Error>.Failure(
                new Error.DatabaseError($"Database error: {ex.Message}"));
        }
    }
}