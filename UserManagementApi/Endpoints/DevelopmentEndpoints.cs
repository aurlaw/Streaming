using Microsoft.Extensions.AI;
using UserManagementApi.Domain;
using UserManagementApi.Services;

namespace UserManagementApi.Endpoints;

/// <summary>
/// Development and testing endpoints (only available in Development environment).
/// </summary>
public static class DevelopmentEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentEndpoints(this IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return app;

        var devGroup = app.MapGroup("/api/dev")
            .WithTags("Development")
            .WithOpenApi();

        // AI Testing Endpoints
        devGroup.MapGet("/test/chat", TestChat)
            .WithName("TestChat")
            .WithSummary("Test OpenAI chat integration");

        devGroup.MapGet("/test/embedding", TestEmbedding)
            .WithName("TestEmbedding")
            .WithSummary("Test OpenAI embedding generation");
        
        // Embedding Management Endpoints
        devGroup.MapPost("/embeddings/generate", GenerateEmbeddings)
            .WithName("GenerateEmbeddings")
            .WithSummary("Generate embeddings for all products that don't have them");
        
        devGroup.MapPost("/embeddings/regenerate", RegenerateEmbeddings)
            .WithName("RegenerateEmbeddings")
            .WithSummary("Regenerate embeddings for ALL products (clears existing ones first)");

        // Prompt Management
        devGroup.MapGet("/prompts/{*promptPath}", GetPrompt)
            .WithName("GetPrompt")
            .WithSummary("View a prompt file");

        devGroup.MapPost("/prompts/clear-cache", ClearPromptCache)
            .WithName("ClearPromptCache")
            .WithSummary("Clear the prompt cache");    
        
        devGroup.MapDelete("/prompts/{*promptPath}", ClearSpecificPromptCache)
            .WithName("ClearSpecificPromptCache")
            .WithSummary("Clear cache for a specific prompt");
        
        return app;
    }

    /// <summary>
    /// Tests OpenAI chat integration.
    /// </summary>
    private static async Task<IResult> TestChat(IChatClient chatClient)
    {
        try
        {
            var response = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Say 'Hello from GPT!' in a friendly way.")]);
            
            return Results.Ok(new 
            { 
                success = true, 
                model = response.ModelId,
                message = response.Text 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"OpenAI chat error: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests OpenAI embedding generation.
    /// </summary>
    private static async Task<IResult> TestEmbedding(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        try
        {
            var testText = "High-performance gaming laptop with RGB keyboard";
            var embeddings = await embeddingGenerator.GenerateAsync([testText]);
            var embedding = embeddings.FirstOrDefault();
            
            return Results.Ok(new 
            { 
                success = true, 
                text = testText,
                dimensions = embedding?.Vector.Length ?? 0,
                firstFewValues = embedding?.Vector.ToArray().Take(5) ?? Array.Empty<float>()
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"OpenAI embedding error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates embeddings for products that don't have them.
    /// </summary>
    private static async Task<IResult> GenerateEmbeddings(IProductEmbeddingService embeddingService)
    {
        try
        {
            var result = await embeddingService.GenerateEmbeddingsForAllProductsAsync();
            
            return result switch
            {
                Result<int, Error>.Success(var count) => 
                    Results.Ok(new 
                    { 
                        success = true, 
                        productsProcessed = count, 
                        message = $"Successfully generated embeddings for {count} products" 
                    }),
                Result<int, Error>.Failure(Error.DatabaseError(var msg)) =>
                    Results.Problem(msg),
                _ => Results.Problem("An unexpected error occurred")
            };
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating embeddings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Regenerates embeddings for all products.
    /// </summary>
    private static async Task<IResult> RegenerateEmbeddings(IProductEmbeddingService embeddingService)
    {
        try
        {
            var result = await embeddingService.RegenerateAllEmbeddingsAsync();
            
            return result switch
            {
                Result<int, Error>.Success(var count) => 
                    Results.Ok(new 
                    { 
                        success = true, 
                        productsProcessed = count, 
                        message = $"Successfully regenerated embeddings for {count} products" 
                    }),
                Result<int, Error>.Failure(Error.DatabaseError(var msg)) =>
                    Results.Problem(msg),
                _ => Results.Problem("An unexpected error occurred")
            };
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error regenerating embeddings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Views a prompt file.
    /// </summary>
    private static async Task<IResult> GetPrompt(string promptPath, IPromptService promptService)
    {
        try
        {
            var content = await promptService.GetPromptAsync(promptPath);
            return Results.Ok(new { promptPath, content });
        }
        catch (FileNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading prompt: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the prompt cache (useful when editing prompts during development).
    /// </summary>
    private static async Task<IResult> ClearPromptCache(IPromptService promptService)
    {
        await promptService.ClearCacheAsync();
        return Results.Ok(new { success = true, message = "Prompt cache cleared (will expire naturally)" });
    }
    /// <summary>
    /// Clears cache for a specific prompt.
    /// </summary>
    private static async Task<IResult> ClearSpecificPromptCache(string promptPath, IPromptService promptService)
    {
        try
        {
            await promptService.ClearPromptCacheAsync(promptPath);
            return Results.Ok(new { success = true, message = $"Cache cleared for prompt: {promptPath}" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error clearing cache: {ex.Message}");
        }
    }
}