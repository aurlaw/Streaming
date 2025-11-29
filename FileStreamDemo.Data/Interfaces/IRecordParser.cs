using FileStreamDemo.Data.Models;

namespace FileStreamDemo.Data.Interfaces;

/// <summary>
/// Interface for parsing records from delimited text lines
/// </summary>
/// <typeparam name="T">The type of record to parse</typeparam>
public interface IRecordParser<T>
{
    /// <summary>
    /// Attempts to parse a record from a line of bytes
    /// </summary>
    /// <param name="line">The line to parse as UTF-8 bytes</param>
    /// <param name="lineNumber">The line number (for error reporting)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>
    /// A tuple containing:
    /// - Record: The parsed record if successful, null otherwise
    /// - Error: An ErrorEvent if parsing failed, null otherwise
    /// </returns>
    ValueTask<(T? Record, ErrorEvent? Error)> TryParseAsync(
        ReadOnlySpan<byte> line,
        int lineNumber,
        CancellationToken cancellationToken = default);
}