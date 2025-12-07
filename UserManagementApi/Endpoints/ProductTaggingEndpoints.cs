using Microsoft.AspNetCore.Mvc;
using UserManagementApi.Domain;
using UserManagementApi.Infrastructure;
using UserManagementApi.Services;

namespace UserManagementApi.Endpoints;

/// <summary>
/// Endpoints for AI-powered product tagging operations.
/// </summary>
public static class ProductTaggingEndpoints
{
    public static void MapProductTaggingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/tagging")
            .WithTags("Product Tagging")
            .WithOpenApi();

        group.MapPost("/tag-all-untagged", TagAllUntaggedProducts)
            .WithName("TagAllUntaggedProducts")
            .WithSummary("Generate tags for all products that don't have tags")
            .Produces<TaggingResultResponse>(200)
            .Produces<ProblemDetails>(500);

        group.MapPost("/tag-all", TagAllProducts)
            .WithName("TagAllProducts")
            .WithSummary("Generate tags for all products, overwriting existing tags")
            .Produces<TaggingResultResponse>(200)
            .Produces<ProblemDetails>(500);
        
        group.MapPost("/{id}/generate-tags", GenerateTagsForProduct)
            .WithName("GenerateTagsForProduct")
            .WithSummary("Generate tags for a specific product")
            .Produces<TagGenerationResponse>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(500);
    }

    private static async Task<IResult> TagAllUntaggedProducts(
        [FromServices] IProductTaggingService taggingService,
        [FromServices] ILogger<Program> logger)
    {
        logger.LogInformation("Received request to tag all untagged products");

        var result = await taggingService.TagAllUntaggedProductsAsync(false);

        return result switch
        {
            Result<int, Error>.Success(var successCount) => Results.Ok(new TaggingResultResponse(
                successCount,
                $"Successfully tagged {successCount} products"
            )),
            Result<int, Error>.Failure(Error.DatabaseError(var msg)) => Results.Problem(
                title: "Tagging Failed",
                detail: msg,
                statusCode: 500
            ),
            Result<int, Error>.Failure(Error.ValidationError(var msg)) => Results.Problem(
                title: "Validation Failed",
                detail: msg,
                statusCode: 400
            ),
            _ => Results.Problem(
                title: "Tagging Failed",
                detail: "An unexpected error occurred",
                statusCode: 500
            )
        };
    }

    private static async Task<IResult> TagAllProducts(
        [FromServices] IProductTaggingService taggingService,
        [FromServices] ILogger<Program> logger)
    {
        logger.LogInformation("Received request to tag all untagged products");

        var result = await taggingService.TagAllUntaggedProductsAsync(true);

        return result switch
        {
            Result<int, Error>.Success(var successCount) => Results.Ok(new TaggingResultResponse(
                successCount,
                $"Successfully tagged {successCount} products"
            )),
            Result<int, Error>.Failure(Error.DatabaseError(var msg)) => Results.Problem(
                title: "Tagging Failed",
                detail: msg,
                statusCode: 500
            ),
            Result<int, Error>.Failure(Error.ValidationError(var msg)) => Results.Problem(
                title: "Validation Failed",
                detail: msg,
                statusCode: 400
            ),
            _ => Results.Problem(
                title: "Tagging Failed",
                detail: "An unexpected error occurred",
                statusCode: 500
            )
        };
    }

    
    private static async Task<IResult> GenerateTagsForProduct(
        string id,
        [FromServices] IProductTaggingService taggingService,
        [FromServices] IProductRepository repository,
        [FromServices] IIdEncoder encoder,
        [FromServices] ILogger<Program> logger)
    {
        logger.LogInformation("Received request to generate tags for product {Id}", id);

        var productIdResult = encoder.DecodeInt(id, EntityIds.Product.Prefix);
        if (productIdResult is Result<int, Error>.Failure(var decodeError))
        {
            return Results.Problem(
                title: "Invalid ID",
                detail: decodeError switch
                {
                    Error.ValidationError(var msg) => msg,
                    _ => "Invalid product ID format"
                },
                statusCode: 400
            );
        }

        var productId = ((Result<int, Error>.Success)productIdResult).Value;
        var productResult = await repository.GetByIdAsync(productId);
        
        if (productResult is Result<Product, Error>.Failure(Error.NotFoundError(var notFoundMsg)))
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Product Not Found",
                Detail = notFoundMsg,
                Status = 404
            });
        }
        
        if (productResult is Result<Product, Error>.Failure(var error))
        {
            return Results.Problem(
                title: "Database Error",
                detail: error switch
                {
                    Error.DatabaseError(var msg) => msg,
                    _ => "Failed to retrieve product"
                },
                statusCode: 500
            );
        }

        var product = ((Result<Product, Error>.Success)productResult).Value;
        var tagsResult = await taggingService.GenerateTagsAsync(product);

        if (tagsResult is Result<List<string>, Error>.Failure(var tagsError))
        {
            return Results.Problem(
                title: "Tag Generation Failed",
                detail: tagsError switch
                {
                    Error.ValidationError(var msg) => msg,
                    _ => "Failed to generate tags"
                },
                statusCode: 500
            );
        }

        var tags = ((Result<List<string>, Error>.Success)tagsResult).Value;

        // Update the product with the generated tags
        var updateResult = await taggingService.UpdateProductTagsAsync(productId, tags);

        return updateResult switch
        {
            Result<Product, Error>.Success(var updatedProduct) => Results.Ok(new TagGenerationResponse(
                encoder.EncodeInt(updatedProduct.Id, EntityIds.Product.Prefix),
                updatedProduct.Name,
                tags,
                string.Join(",", tags)
            )),
            Result<Product, Error>.Failure(Error.NotFoundError(var msg)) => Results.NotFound(new ProblemDetails
            {
                Title = "Product Not Found",
                Detail = msg,
                Status = 404
            }),
            Result<Product, Error>.Failure(Error.DatabaseError(var msg)) => Results.Problem(
                title: "Update Failed",
                detail: msg,
                statusCode: 500
            ),
            _ => Results.Problem(
                title: "Update Failed",
                detail: "An unexpected error occurred",
                statusCode: 500
            )
        };
    }
}

/// <summary>
/// Response for bulk tagging operation.
/// </summary>
public record TaggingResultResponse(
    int ProductsTagged,
    string Message
);

/// <summary>
/// Response for single product tag generation.
/// </summary>
public record TagGenerationResponse(
    string ProductId,
    string ProductName,
    List<string> Tags,
    string TagsString
);