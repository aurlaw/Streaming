using Microsoft.Extensions.AI;

namespace VoiceEssay.Endpoints;

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
        // devGroup.MapGet("/test/chat", TestChat)
        //     .WithName("TestChat")
        //     .WithSummary("Test OpenAI chat integration");

        devGroup.MapGet("/test/embedding", TestEmbedding)
            .WithName("TestEmbedding")
            .WithSummary("Test OpenAI embedding generation");
        
        // // Embedding Management Endpoints
        // devGroup.MapPost("/embeddings/generate", GenerateEmbeddings)
        //     .WithName("GenerateEmbeddings")
        //     .WithSummary("Generate embeddings for all products that don't have them");
        
        // devGroup.MapPost("/embeddings/regenerate", RegenerateEmbeddings)
        //     .WithName("RegenerateEmbeddings")
        //     .WithSummary("Regenerate embeddings for ALL products (clears existing ones first)");
        
        
        return app;
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
}