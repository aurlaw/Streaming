using UserManagementApi.Domain;
using UserManagementApi.DTOs;
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
        Price = entity.Price
    };
    
    /// <summary>
    /// Converts a domain model to a database entity.
    /// </summary>
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price
    };
    
    /// <summary>
    /// Converts a domain model to an API response DTO.
    /// </summary>
    public static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Price
    );
}