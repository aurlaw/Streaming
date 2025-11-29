using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using FileStreamDemo.Data.Interfaces;
using FileStreamDemo.Data.Models;
using Microsoft.Extensions.Logging;

namespace FileStreamDemo.Data.Services;

public class FileParserService<T>
{
    private readonly ILogger<FileParserService<T>> _logger;
    private readonly IRecordParser<T> _parser;

    public FileParserService(ILogger<FileParserService<T>> logger, IRecordParser<T> parser)
    {
        _logger = logger;
        _parser = parser;
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

        var stopwatch = Stopwatch.StartNew();
        var state = new ProcessingState
        {
            LineNumber = 0,
            TotalRecords = 0,
            ErrorCount = 0,
            RecordsSinceLastProgress = 0,
            LastProgressTime = stopwatch.Elapsed
        };
        var currentBatch = new List<T>(options.BatchSize);

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
                        state.LineNumber++;
                        var parseResult = await _parser.TryParseAsync(leftoverBytes.ToArray(), state.LineNumber, cancellationToken);
    
                        if (parseResult.Record != null)
                        {
                            currentBatch.Add(parseResult.Record);
                            state.TotalRecords++;
                            state.RecordsSinceLastProgress++;
                        }
                        else if (parseResult.Error != null)
                        {
                            state.ErrorCount++;
                            yield return parseResult.Error;
                        }
                    }
                    break;
                }

                _logger.LogTrace("Read {BytesRead} bytes from file at position {Position}", 
                    bytesRead, fileStream.Position);

                var eventsToYield = await ProcessChunkAsync(
                    buffer,
                    bytesRead,
                    leftoverBytes,
                    currentBatch,
                    state,
                    stopwatch,
                    fileStream.Position,
                    fileSize,
                    options,
                    cancellationToken);

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

        // Yield final batch
        if (options.YieldBatchEvents && currentBatch.Count > 0)
        {
            _logger.LogDebug("Yielding final batch of {BatchSize} records", currentBatch.Count);
            
            yield return new BatchParsedEvent<T>
            {
                Records = currentBatch,
                TotalProcessedSoFar = state.TotalRecords
            };
        }

        stopwatch.Stop();
        
        _logger.LogInformation("Parsing completed. Total records: {TotalRecords}, Errors: {ErrorCount}, Duration: {Duration}", 
            state.TotalRecords, state.ErrorCount, stopwatch.Elapsed);

        yield return new CompletionEvent
        {
            TotalRecords = state.TotalRecords,
            Duration = stopwatch.Elapsed,
            ErrorCount = state.ErrorCount,
        };
    }
    
    private async Task<List<ParserEvent>> ProcessChunkAsync(
        byte[] chunkData,
        int chunkLength,
        List<byte> leftoverBytes,
        List<T> currentBatch,
        ProcessingState state,
        Stopwatch stopwatch,
        long filePosition,
        long fileSize,
        ParserOptions options,
        CancellationToken cancellationToken)
    {
        var events = new List<ParserEvent>();
        
        // Combine leftover bytes from previous read with new bytes
        byte[] dataBuffer;
        int dataLength;

        if (leftoverBytes.Count > 0)
        {
            _logger.LogTrace("Combining {LeftoverCount} leftover bytes with {NewBytes} new bytes", 
                leftoverBytes.Count, chunkLength);
            
            dataBuffer = new byte[leftoverBytes.Count + chunkLength];
            leftoverBytes.CopyTo(dataBuffer);
            Array.Copy(chunkData, 0, dataBuffer, leftoverBytes.Count, chunkLength);
            dataLength = dataBuffer.Length;
            leftoverBytes.Clear();
        }
        else
        {
            dataBuffer = chunkData;
            dataLength = chunkLength;
        }

        // Extract all lines first (synchronous, no await)
        var linesToParse = new List<(byte[] LineData, int LineNumber)>();
        ReadOnlySpan<byte> dataSpan = dataBuffer.AsSpan(0, dataLength);

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

            state.LineNumber++;
            
            // Copy line to array for async processing
            linesToParse.Add((lineSpan.ToArray(), state.LineNumber));
        }

        // Now parse all lines (can await here, no spans in scope)
        foreach (var (lineData, lineNum) in linesToParse)
        {
            var parseResult = await _parser.TryParseAsync(lineData, lineNum, cancellationToken);
            
            if (parseResult.Record != null)
            {
                currentBatch.Add(parseResult.Record);
                state.TotalRecords++;
                state.RecordsSinceLastProgress++;

                // Yield batch if we've reached batch size
                if (options.YieldBatchEvents && currentBatch.Count >= options.BatchSize)
                {
                    _logger.LogDebug("Yielding batch of {BatchSize} records. Total so far: {TotalRecords}", 
                        currentBatch.Count, state.TotalRecords);
                    
                    events.Add(new BatchParsedEvent<T>
                    {
                        Records = new List<T>(currentBatch),
                        TotalProcessedSoFar = state.TotalRecords
                    });
                    currentBatch.Clear();
                }
            }
            else if (parseResult.Error != null)
            {
                state.ErrorCount++;
                events.Add(parseResult.Error);
            }

            // Check if we should yield a progress event
            var shouldYieldProgress = options.YieldProgressEvents && (
                state.RecordsSinceLastProgress >= options.ProgressRecordInterval ||
                (stopwatch.Elapsed - state.LastProgressTime).TotalMilliseconds >= options.ProgressIntervalMs
            );

            if (shouldYieldProgress)
            {
                var percentComplete = fileSize > 0 
                    ? (double)filePosition / fileSize * 100 
                    : 0;

                _logger.LogInformation("Progress: {RecordsProcessed} records ({PercentComplete:F2}%), Elapsed: {Elapsed}", 
                    state.TotalRecords, percentComplete, stopwatch.Elapsed);

                events.Add(new ProgressEvent
                {
                    RecordsProcessed = state.TotalRecords,
                    PercentComplete = percentComplete,
                    Elapsed = stopwatch.Elapsed
                });

                state.LastProgressTime = stopwatch.Elapsed;
                state.RecordsSinceLastProgress = 0;
            }
        }

        return events;
    }
    
    private class ProcessingState
    {
        public int LineNumber { get; set; }
        public int TotalRecords { get; set; }
        public int ErrorCount { get; set; }
        public int RecordsSinceLastProgress { get; set; }
        public TimeSpan LastProgressTime { get; set; }
    }
}
