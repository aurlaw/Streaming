using LegalAnalysisAPI.Domain;

namespace LegalAnalysisAPI.Extensions;

/// <summary>
/// Extension methods for chaining asynchronous Result operations.
/// </summary>
public static class AsyncResultExtensions
{
    /// <summary>
    /// Converts a synchronous Result into a Task-wrapped Result for use in async pipelines.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="result">The synchronous result to wrap.</param>
    /// <returns>A completed Task containing the result.</returns>
    /// <remarks>
    /// This is a bridge method to transition from synchronous to asynchronous operations
    /// in a result pipeline. The returned Task is already completed.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await ParseUserId(input)  // synchronous
    ///     .ToAsync()                         // bridge to async
    ///     .ThenAsync(id => FetchUserAsync(id));  // asynchronous
    /// </code>
    /// </example>
    public static Task<Result<T, E>> ToAsync<T, E>(this Result<T, E> result) =>
        Task.FromResult(result);
    
    /// <summary>
    /// Chains an asynchronous operation that returns a Result, propagating failures automatically.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="U">The type of the next success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="resultTask">The current async result.</param>
    /// <param name="next">An async function to execute if the current result is successful.</param>
    /// <returns>
    /// If the current result is Success, returns the result of calling next with the success value.
    /// If the current result is Failure, returns a new Failure with the same error.
    /// </returns>
    /// <remarks>
    /// This is the async equivalent of Then. Use this when the next operation in the pipeline
    /// is asynchronous (database calls, HTTP requests, I/O operations, etc.).
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await ParseUserId(input)
    ///     .ToAsync()
    ///     .ThenAsync(id => FetchUserFromDatabaseAsync(id))
    ///     .ThenAsync(user => SendEmailAsync(user));
    /// </code>
    /// </example>
    public static async Task<Result<U, E>> ThenAsync<T, U, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task<Result<U, E>>> next)
    {
        var result = await resultTask;
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return result switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<T, E>.Success(var value) => await next(value),
            Result<T, E>.Failure(var error) => new Result<U, E>.Failure(error)
        };
    }
    
    /// <summary>
    /// Transforms the success value of an async Result using an async transformation function.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="U">The type of the transformed success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="resultTask">The current async result.</param>
    /// <param name="mapper">An async function to transform the success value.</param>
    /// <returns>
    /// If the current result is Success, returns Success with the transformed value.
    /// If the current result is Failure, returns a new Failure with the same error.
    /// </returns>
    /// <remarks>
    /// Use this when your transformation itself is asynchronous but cannot fail.
    /// For example, calling an external service to enrich data.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await FetchUserAsync(id)
    ///     .MapAsync(user => EnrichWithProfileDataAsync(user))
    ///     .MapAsync(enriched => enriched.DisplayName);
    /// </code>
    /// </example>
    public static async Task<Result<U, E>> MapAsync<T, U, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task<U>> mapper)
    {
        var result = await resultTask;
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return result switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<T, E>.Success(var value) => 
                new Result<U, E>.Success(await mapper(value)),
            Result<T, E>.Failure(var error) => 
                new Result<U, E>.Failure(error)
        };
    }
    
    /// <summary>
    /// Transforms the success value of an async Result using a synchronous transformation function.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="U">The type of the transformed success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="resultTask">The current async result.</param>
    /// <param name="mapper">A synchronous function to transform the success value.</param>
    /// <returns>
    /// If the current result is Success, returns Success with the transformed value.
    /// If the current result is Failure, returns a new Failure with the same error.
    /// </returns>
    /// <remarks>
    /// Use this when you're in an async pipeline but need to apply a simple, 
    /// synchronous transformation (like extracting a property or converting types).
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await FetchUserAsync(id)
    ///     .MapAsync(user => user.Name)  // Synchronous property access
    ///     .MapAsync(name => name.ToUpper());
    /// </code>
    /// </example>
    public static async Task<Result<U, E>> MapAsync<T, U, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, U> mapper)
    {
        var result = await resultTask;
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return result switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<T, E>.Success(var value) => 
                new Result<U, E>.Success(mapper(value)),
            Result<T, E>.Failure(var error) => 
                new Result<U, E>.Failure(error)
        };
    }
}