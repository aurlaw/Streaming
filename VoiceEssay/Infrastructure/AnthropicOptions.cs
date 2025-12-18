namespace VoiceEssay.Infrastructure;
/// <summary>
/// Configuration options for Anthropic integration.
/// </summary>
public class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    
    /// <summary>
    /// OpenAI API key (stored in user secrets).
    /// </summary>
    public string ApiKey { get; set; }  = null!;

    public string Model { get; set; } = null!;
    /// <summary>
    /// Maximum tokens for chat completions.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
    
}
