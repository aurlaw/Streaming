using LegalAnalysisAPI.Domain;

namespace LegalAnalysisAPI.Extensions;

/// <summary>
/// Extension methods for chaining Result operations in a railway-oriented programming style.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Chains an operation that returns a Result, propagating failures automatically.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="U">The type of the next success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="next">A function to execute if the current result is successful.</param>
    /// <returns>
    /// If the current result is Success, returns the result of calling next with the success value.
    /// If the current result is Failure, returns a new Failure with the same error.
    /// </returns>
    /// <remarks>
    /// This is the core method for chaining operations that might fail. Failures short-circuit
    /// the pipeline, preventing subsequent operations from executing.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = ParseUserId(input)
    ///     .Then(id => FetchUser(id))
    ///     .Then(user => ValidateUser(user));
    /// </code>
    /// </example>
    public static Result<U, E> Then<T, U, E>(
        this Result<T, E> result,
        Func<T, Result<U, E>> next) =>
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        result switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<T, E>.Success(var value) => next(value),
            Result<T, E>.Failure(var error) => new Result<U, E>.Failure(error)
        };
    
    /// <summary>
    /// Transforms the success value of a Result without the possibility of failure.
    /// </summary>
    /// <typeparam name="T">The type of the current success value.</typeparam>
    /// <typeparam name="U">The type of the transformed success value.</typeparam>
    /// <typeparam name="E">The type of error.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>
    /// If the current result is Success, returns Success with the transformed value.
    /// If the current result is Failure, returns a new Failure with the same error.
    /// </returns>
    /// <remarks>
    /// Use Map when you need to transform a value but the transformation cannot fail.
    /// For transformations that might fail, use Then instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = FetchUser(id)
    ///     .Map(user => user.Name)
    ///     .Map(name => name.ToUpper());
    /// </code>
    /// </example>
    public static Result<U, E> Map<T, U, E>(
        this Result<T, E> result,
        Func<T, U> mapper) =>
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        result switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            Result<T, E>.Success(var value) => 
                new Result<U, E>.Success(mapper(value)),
            Result<T, E>.Failure(var error) => 
                new Result<U, E>.Failure(error)
        };
}