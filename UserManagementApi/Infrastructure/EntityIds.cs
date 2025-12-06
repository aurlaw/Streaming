namespace UserManagementApi.Infrastructure;

/// <summary>
/// Entity ID configuration - prefixes and route patterns for API encoding.
/// This is an API-layer concern and does not affect domain models.
/// </summary>
public static class EntityIds
{
    public static class User
    {
        public const string Prefix = "u";
        public const string RoutePattern = "/api/users";
    }
    
    public static class Product
    {
        public const string Prefix = "p";
        public const string RoutePattern = "/api/products";
    }
    
    // Add new entities here as needed:
    // public static class Order
    // {
    //     public const string Prefix = "o";
    //     public const string RoutePattern = "/api/orders";
    // }
    
    /// <summary>
    /// Determines the ID prefix from a route path.
    /// </summary>
    public static string GetPrefixFromRoute(string path)
    {
        if (path.Contains(User.RoutePattern, StringComparison.OrdinalIgnoreCase))
            return User.Prefix;
        
        if (path.Contains(Product.RoutePattern, StringComparison.OrdinalIgnoreCase))
            return Product.Prefix;
        
        return string.Empty;
    }
}