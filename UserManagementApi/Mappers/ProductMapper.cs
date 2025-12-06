using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Infrastructure;
using UserManagementApi.Infrastructure.Entities;

namespace UserManagementApi.Mappers;
/// <summary>
/// Maps between Product domain models, entities, and DTOs.
/// </summary>
public static class ProductMapper
{
    /// <summary>
    /// Converts a database entity to a domain model.
    /// </summary>
    public static Product ToDomain(ProductEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Category = entity.Category,
        Brand = entity.Brand,
        Price = entity.Price,
        Stock = entity.Stock,
        IsActive = entity.IsActive,
        Rating = entity.Rating,
        ReviewCount = entity.ReviewCount,
        Tags = entity.Tags
    };
    
    /// <summary>
    /// Converts a domain model to a database entity.
    /// </summary>
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Category = product.Category,
        Brand = product.Brand,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive,
        Rating = product.Rating,
        ReviewCount = product.ReviewCount,
        Tags = product.Tags
    };
    
    /// <summary>
    /// Converts a domain model to an API response DTO.
    /// </summary>
    public static ProductResponse ToResponse(Product product, IIdEncoder encoder)
    {
        var encodedId = encoder.EncodeInt(product.Id, EntityIds.Product.Prefix);
        return new ProductResponse(
            encodedId,
            product.Name,
            product.Description,
            product.Category,
            product.Brand,
            product.Price,
            product.Stock,
            product.IsActive,
            product.Rating,
            product.ReviewCount,
            product.Tags
        );
    }
}