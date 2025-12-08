namespace LegalAnalysisAPI.Infrastructure;

/// <summary>
/// Configuration options for OpenAI integration.
/// </summary>
public class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    
    /// <summary>
    /// OpenAI API key (stored in user secrets).
    /// </summary>
    public string ApiKey { get; set; }  = null!;
    
    
    /// <summary>
    /// Embedding model to use.
    /// </summary>
    public string EmbeddingModel { get; set; } = null!;
    
    /// <summary>
    /// Dimensions for embeddings (1536 for text-embedding-3-small).
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;
    
    /// <summary>
    /// Maximum tokens for chat completions.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
    
    /// <summary>
    /// Temperature for chat responses (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}