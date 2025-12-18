namespace VoiceEssay.Infrastructure;
/// <summary>
/// Configuration options for application.
/// </summary>
public class VoiceEssayOptions
{
    public const string SectionName = "VoiceToEssay";
    
    public string StoragePath { get; set; }  = null!;
    public double JobExpirationHours { get; set; }
    public double CleanupIntervalMinutes { get; set; } 
    public double MaxFileSizeMB { get; set; } 

    
}