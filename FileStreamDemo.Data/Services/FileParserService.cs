using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using FileStreamDemo.Data.Models;
using Microsoft.Extensions.Logging;

namespace FileStreamDemo.Data.Services;

public class FileParserService
{
    private readonly ILogger<FileParserService> _logger;

    public FileParserService(ILogger<FileParserService> logger)
    {
        _logger = logger;
    }
    
    public async IAsyncEnumerable<ParserEvent> ParseFileAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        ParserOptions? options = null)
    {
        options ??= new ParserOptions();
        
        _logger.LogInformation("Starting file parsing: {FilePath} with BufferSize: {BufferSize}, BatchSize: {BatchSize}", 
            filePath, options.BufferSize, options.BatchSize);
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            yield return new ErrorEvent
            {
                Message = "File not found",
                LineNumber = 0,
                Exception = new FileNotFoundException("File not found", filePath)
            };
            yield break;
        }

        // Call the internal method that does the actual work
        await foreach (var evt in ParseFileInternalAsync(filePath, cancellationToken, options))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<ParserEvent> ParseFileInternalAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        ParserOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastProgressTime = stopwatch.Elapsed;
        var recordsSinceLastProgress = 0;
        var totalRecords = 0;
        var errorCount = 0;
        var lineNumber = 0;
        var currentBatch = new List<Person>(options.BatchSize);

        var buffer = ArrayPool<byte>.Shared.Rent(options.BufferSize);
        var leftoverBytes = new List<byte>();

        _logger.LogDebug("Rented buffer of size {BufferSize} from ArrayPool", buffer.Length);

        FileStream? fileStream = null;

        try
        {
            fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1,
                useAsync: true);

            var fileSize = fileStream.Length;
            _logger.LogInformation("File size: {FileSize} bytes", fileSize);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, options.BufferSize), cancellationToken);
                
                if (bytesRead == 0)
                {
                    _logger.LogDebug("Reached end of file");
                    
                    if (leftoverBytes.Count > 0)
                    {
                        _logger.LogDebug("Processing {Count} leftover bytes", leftoverBytes.Count);
                        lineNumber++;
                        var parseResult = TryParseLine(leftoverBytes.ToArray(), lineNumber);
        
                        if (parseResult.Person != null)
                        {
                            currentBatch.Add(parseResult.Person);
                            totalRecords++;
                            recordsSinceLastProgress++;
                        }
                        else if (parseResult.Error != null)
                        {
                            errorCount++;
                            yield return parseResult.Error;
                        }                    }
                    
                    break;
                }

                _logger.LogTrace("Read {BytesRead} bytes from file at position {Position}", 
                    bytesRead, fileStream.Position);

                var eventsToYield = ProcessChunk(
                    buffer.AsSpan(0, bytesRead),
                    leftoverBytes,
                    currentBatch,
                    ref lineNumber,
                    ref totalRecords,
                    ref errorCount,
                    ref recordsSinceLastProgress,
                    ref lastProgressTime,
                    stopwatch,
                    fileStream.Position,
                    fileSize,
                    options);

                foreach (var evt in eventsToYield)
                {
                    yield return evt;
                }
            }
        }
        finally
        {
            fileStream?.Dispose();
            ArrayPool<byte>.Shared.Return(buffer);
            _logger.LogDebug("Returned buffer to ArrayPool");
        }

        // These yields are outside the try block
        if (options.YieldBatchEvents && currentBatch.Count > 0)
        {
            _logger.LogDebug("Yielding final batch of {BatchSize} records", currentBatch.Count);
            
            yield return new BatchParsedEvent
            {
                People = currentBatch,
                TotalProcessedSoFar = totalRecords
            };
        }

        stopwatch.Stop();
        
        _logger.LogInformation("Parsing completed. Total records: {TotalRecords}, Errors: {ErrorCount}, Duration: {Duration}", 
            totalRecords, errorCount, stopwatch.Elapsed);

        yield return new CompletionEvent
        {
            TotalRecords = totalRecords,
            Duration = stopwatch.Elapsed,
            ErrorCount = errorCount
        };
    }    
    
    private List<ParserEvent> ProcessChunk(
        ReadOnlySpan<byte> chunkData,
        List<byte> leftoverBytes,
        List<Person> currentBatch,
        ref int lineNumber,
        ref int totalRecords,
        ref int errorCount,
        ref int recordsSinceLastProgress,
        ref TimeSpan lastProgressTime,
        Stopwatch stopwatch,
        long filePosition,
        long fileSize,
        ParserOptions options)
    {
        var events = new List<ParserEvent>();
        
        // Combine leftover bytes from previous read with new bytes
        ReadOnlySpan<byte> dataSpan;

        if (leftoverBytes.Count > 0)
        {
            _logger.LogTrace("Combining {LeftoverCount} leftover bytes with {NewBytes} new bytes", 
                leftoverBytes.Count, chunkData.Length);
            
            var combinedBuffer = new byte[leftoverBytes.Count + chunkData.Length];
            leftoverBytes.CopyTo(combinedBuffer);
            chunkData.CopyTo(combinedBuffer.AsSpan(leftoverBytes.Count));
            dataSpan = combinedBuffer;
            leftoverBytes.Clear();
        }
        else
        {
            dataSpan = chunkData;
        }

        // Process complete lines in this chunk
        while (dataSpan.Length > 0)
        {
            var newlineIndex = dataSpan.IndexOf((byte)'\n');

            if (newlineIndex < 0)
            {
                // No complete line found - save for next iteration
                _logger.LogTrace("No complete line found, saving {Count} bytes for next iteration", 
                    dataSpan.Length);
                leftoverBytes.AddRange(dataSpan.ToArray());
                break;
            }

            // Extract line (handle \r\n or \n)
            var lineSpan = dataSpan.Slice(0, newlineIndex);
            if (lineSpan.Length > 0 && lineSpan[^1] == (byte)'\r')
            {
                lineSpan = lineSpan.Slice(0, lineSpan.Length - 1);
            }

            // Move past this line
            dataSpan = dataSpan.Slice(newlineIndex + 1);

            if (lineSpan.Length == 0)
                continue; // Skip empty lines

            lineNumber++;

            // Parse the line using spans
            var parseResult = TryParseLine(lineSpan, lineNumber);
            
            if (parseResult.Person != null)
            {
                currentBatch.Add(parseResult.Person);
                totalRecords++;
                recordsSinceLastProgress++;

                // Yield batch if we've reached batch size
                if (options.YieldBatchEvents && currentBatch.Count >= options.BatchSize)
                {
                    _logger.LogDebug("Yielding batch of {BatchSize} records. Total so far: {TotalRecords}", 
                        currentBatch.Count, totalRecords);
                    
                    events.Add(new BatchParsedEvent
                    {
                        People = new List<Person>(currentBatch),
                        TotalProcessedSoFar = totalRecords
                    });
                    currentBatch.Clear();
                }
            }
            else if (parseResult.Error != null)
            {
                errorCount++;
                events.Add(parseResult.Error);
            }

            // Check if we should yield a progress event
            var shouldYieldProgress = options.YieldProgressEvents && (
                recordsSinceLastProgress >= options.ProgressRecordInterval ||
                (stopwatch.Elapsed - lastProgressTime).TotalMilliseconds >= options.ProgressIntervalMs
            );

            if (!shouldYieldProgress) continue;
            var percentComplete = fileSize > 0 
                ? (double)filePosition / fileSize * 100 
                : 0;

            _logger.LogInformation("Progress: {RecordsProcessed} records ({PercentComplete:F2}%), Elapsed: {Elapsed}", 
                totalRecords, percentComplete, stopwatch.Elapsed);

            events.Add(new ProgressEvent
            {
                RecordsProcessed = totalRecords,
                PercentComplete = percentComplete,
                Elapsed = stopwatch.Elapsed
            });

            lastProgressTime = stopwatch.Elapsed;
            recordsSinceLastProgress = 0;
        }

        return events;
    }
        
    private (Person? Person, ErrorEvent? Error) TryParseLine(ReadOnlySpan<byte> line, int lineNumber)
    {
        try
        {
            var person = ParsePersonFromLine(line);
            return (person, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {LineContent}", 
                lineNumber, Encoding.UTF8.GetString(line));
        
            return (null, new ErrorEvent
            {
                Message = $"Failed to parse line {lineNumber}: {ex.Message}",
                LineNumber = lineNumber,
                Exception = ex
            });
        }
    }

    private Person ParsePersonFromLine(ReadOnlySpan<byte> line)
    {
        // Find first comma
        var firstComma = line.IndexOf((byte)',');
        if (firstComma < 0) 
            throw new FormatException("Invalid line format - missing first comma");

        // Extract first name
        var firstNameBytes = line.Slice(0, firstComma);
        var firstName = Encoding.UTF8.GetString(firstNameBytes);

        // Move past first comma
        line = line.Slice(firstComma + 1);

        // Find second comma
        var secondComma = line.IndexOf((byte)',');
        if (secondComma < 0) 
            throw new FormatException("Invalid line format - missing second comma");

        // Extract last name
        var lastNameBytes = line.Slice(0, secondComma);
        var lastName = Encoding.UTF8.GetString(lastNameBytes);

        // Extract date (remaining part)
        var dateBytes = line.Slice(secondComma + 1);
        var dateString = Encoding.UTF8.GetString(dateBytes);

        if (!DateOnly.TryParse(dateString, out var birthDate))
            throw new FormatException("Invalid date format");

        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate
        };
    }
     
}
