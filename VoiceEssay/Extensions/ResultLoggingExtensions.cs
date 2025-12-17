using VoiceEssay.Domain;

namespace VoiceEssay.Extensions;

/// <summary>
/// Extension methods for adding logging to Result pipelines.
/// </summary>
public static class ResultLoggingExtensions
{
    /// <summary>
    /// Logs a success message when the Result is successful, then returns the result unchanged.
    /// </summary>
    public static async Task<Result<T, E>> LogSuccess<T, E>(
        this Task<Result<T, E>> resultTask,
        ILogger logger,
        string message,
        params object[] args)
    {
        var result = await resultTask;
        
        if (result is Result<T, E>.Success)
            logger.LogInformation(message, args);
        
        return result;
    }
    
    /// <summary>
    /// Logs a warning message when the Result is a failure, then returns the result unchanged.
    /// </summary>
    public static async Task<Result<T, E>> LogFailure<T, E>(
        this Task<Result<T, E>> resultTask,
        ILogger logger,
        string message,
        params object[] args)
    {
        var result = await resultTask;
        
        if (result is Result<T, E>.Failure(var error))
        {
            var allArgs = args.Concat(new object[] { error! }).ToArray();
            logger.LogWarning(message + " Error: {Error}", allArgs);
        }
        
        return result;
    }
} 