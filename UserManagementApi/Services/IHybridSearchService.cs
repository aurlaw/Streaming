using UserManagementApi.Domain;
using UserManagementApi.DTOs;

namespace UserManagementApi.Services;

/// <summary>
/// Service for hybrid search combining structured filters and semantic ranking.
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Searches products using hybrid approach (structured + semantic).
    /// </summary>
    Task<Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>> 
        SearchAsync(string query, int limit);
}