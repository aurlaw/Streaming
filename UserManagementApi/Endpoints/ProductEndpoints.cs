using UserManagementApi.Domain;
using UserManagementApi.DTOs;
using UserManagementApi.Extensions;
using UserManagementApi.Infrastructure;
using UserManagementApi.Mappers;
using UserManagementApi.Services;

namespace UserManagementApi.Endpoints;

/// <summary>
/// Endpoints for product management and search operations.
/// </summary>
public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetPagedProducts)
            .WithName("GetPagedProducts")
            .WithSummary("Get paginated products")
            .Produces<PagedProductsResponse>(200)
            .Produces(400);

        group.MapGet("/{encodedId}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .Produces<ProductResponse>(200)
            .Produces(400)
            .Produces(404);
        
        group.MapPost("/search/natural", SearchWithNaturalLanguage)
            .WithName("SearchWithNaturalLanguage")
            .WithSummary("Search products using natural language")
            .Produces<NaturalLanguageSearchResponse>(200)
            .Produces(400);
        
        group.MapPost("/search", SearchHybrid)
            .WithName("SearchHybrid")
            .WithSummary("Search products using hybrid approach (structured + semantic)")
            .Produces<HybridSearchResponse>(200)
            .Produces(400);
        
        return group;
    }

    /// <summary>
    /// Retrieves paginated products.
    /// </summary>
    private static async Task<IResult> GetPagedProducts(
        [AsParameters] GetProductsRequest request,
        ProductService productService,
        IIdEncoder encoder)
    {
        var result = await productService.GetPagedProductsAsync(request);
        
        return result switch
        {
            Result<PagedResult<Product>, Error>.Success(var page) =>
                MapPagedResponse(page, encoder),
            Result<PagedResult<Product>, Error>.Failure(Error.ValidationError(var msg)) =>
                Results.BadRequest(new { error = msg }),
            Result<PagedResult<Product>, Error>.Failure(Error.DatabaseError(var msg)) =>
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };
    }

    private static IResult MapPagedResponse(PagedResult<Product> page, IIdEncoder encoder)
    {
        var productResponses = page.Items.Select(p => ProductMapper.ToResponse(p, encoder));
        var nextCursor = page.NextId.HasValue 
            ? encoder.EncodeInt(page.NextId.Value, EntityIds.Product.Prefix)
            : null;
        
        var response = new PagedProductsResponse(
            productResponses,
            nextCursor,
            page.HasMore
        );
        
        return Results.Ok(response);
    }

    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    private static async Task<IResult> GetProductById(string encodedId, ProductService productService, IIdEncoder encoder)
    {
        var result = await productService.GetProductByEncodedIdAsync(encodedId)
            .MapAsync(product => ProductMapper.ToResponse(product, encoder));
        
        return result switch
        {
            Result<ProductResponse, Error>.Success(var response) => 
                Results.Ok(response),
            Result<ProductResponse, Error>.Failure(Error.ValidationError(var msg)) => 
                Results.BadRequest(new { error = msg }),
            Result<ProductResponse, Error>.Failure(Error.NotFoundError(var msg)) => 
                Results.NotFound(new { error = msg }),
            Result<ProductResponse, Error>.Failure(Error.DatabaseError(var msg)) => 
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };
    }
    
    /// <summary>
    /// Searches products using natural language query.
    /// </summary>
    private static async Task<IResult> SearchWithNaturalLanguage(
        NaturalLanguageSearchRequest request,
        INaturalLanguageSearchService searchService,
        IIdEncoder encoder)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty" });
        }
    
        var result = await searchService.SearchAsync(request.Query, request.PageSize);
    
        if (result is Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>.Success success)
        {
            var productResponses = success.Value.Products.Select(p => ProductMapper.ToResponse(p, encoder));
            var response = new NaturalLanguageSearchResponse
            {
                Products = productResponses,
                Interpretation = success.Value.Interpretation,
                AppliedFilters = success.Value.Filters,
                ResultCount = success.Value.Products.Count()
            };
        
            return Results.Ok(response);
        }
    
        return result switch
        {
            Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(Error.ValidationError(var msg)) =>
                Results.BadRequest(new { error = msg }),
            Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(Error.DatabaseError(var msg)) =>
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };        
    }
    
    /// <summary>
    /// Searches products using hybrid approach (structured filters + semantic ranking).
    /// </summary>
    private static async Task<IResult> SearchHybrid(
        HybridSearchRequest request,
        IHybridSearchService searchService,
        IIdEncoder encoder)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty" });
        }
    
        var result = await searchService.SearchAsync(request.Query, request.PageSize);
    
        if (result is Result<(IEnumerable<Product> Products, ProductSearchFilters Filters, string Interpretation), Error>.Success success)
        {
            var productResponses = success.Value.Products.Select(p => ProductMapper.ToResponse(p, encoder));
            var response = new HybridSearchResponse
            {
                Products = productResponses,
                Interpretation = success.Value.Interpretation,
                AppliedFilters = success.Value.Filters,
                ResultCount = success.Value.Products.Count(),
                SearchMode = "hybrid"
            };
        
            return Results.Ok(response);
        }
    
        return result switch
        {
            Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(Error.ValidationError(var msg)) =>
                Results.BadRequest(new { error = msg }),
            Result<(IEnumerable<Product>, ProductSearchFilters, string), Error>.Failure(Error.DatabaseError(var msg)) =>
                Results.Problem(msg),
            _ => Results.Problem("An unexpected error occurred")
        };
    }
    
}
