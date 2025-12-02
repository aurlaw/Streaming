namespace UserManagementApi.Domain;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <typeparam name="E">The type of the error value.</typeparam>
/// <remarks>
/// This is an implementation of the Result pattern (also called Either in functional programming).
/// It provides type-safe error handling without exceptions, making error paths explicit in the code.
/// </remarks>
public abstract record Result<T, E>
{
    /// <summary>
    /// Represents a successful operation with a resulting value.
    /// </summary>
    /// <param name="Value">The value produced by the successful operation.</param>
    public record Success(T Value) : Result<T, E>;
    
    /// <summary>
    /// Represents a failed operation with an error.
    /// </summary>
    /// <param name="Error">The error that caused the operation to fail.</param>
    public record Failure(E Error) : Result<T, E>;
    
    private Result() { } // Sealed hierarchy - can only be Success or Failure
}