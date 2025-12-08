namespace LegalAnalysisAPI.Domain;

/// <summary>
/// Represents different types of errors that can occur in the application.
/// </summary>
public abstract record Error
{
    /// <summary>
    /// Represents an error caused by invalid input or business rule violation.
    /// </summary>
    /// <param name="Message">A description of what validation failed.</param>
    public record ValidationError(string Message) : Error;
    
    /// <summary>
    /// Represents an error when a requested resource cannot be found.
    /// </summary>
    /// <param name="Message">A description of what was not found.</param>
    public record NotFoundError(string Message) : Error;
    
    /// <summary>
    /// Represents an error that occurred during database operations.
    /// </summary>
    /// <param name="Message">A description of the database error.</param>
    public record DatabaseError(string Message) : Error;
    
    /// <summary>
    /// Represents an error when attempting to create a duplicate resource.
    /// </summary>
    /// <param name="Message">A description of the duplicate conflict.</param>
    public record DuplicateError(string Message) : Error;
}