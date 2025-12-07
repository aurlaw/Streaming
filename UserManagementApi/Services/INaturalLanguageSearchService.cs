using UserManagementApi.Domain;
using UserManagementApi.DTOs;

namespace UserManagementApi.Services;

/// <summary>
/// Service for natural language product search using AI.
/// </summary>
public interface INaturalLanguageSearchService
{
    /// <summary>
    /// Searches products using natural language query.
    /// </summary>
    Task<Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>> 
        SearchAsync(string query, int limit);
}