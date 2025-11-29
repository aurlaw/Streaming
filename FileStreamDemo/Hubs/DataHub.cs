using System.Collections.Concurrent;
using FileStreamDemo.Data.Models;
using FileStreamDemo.Data.Services;
using Microsoft.AspNetCore.SignalR;

namespace FileStreamDemo.Hubs;

public class DataHub : Hub
{
    private readonly ILogger<DataHub> _logger;
    private readonly FileParserService _fileParserService;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<DataHub> _hubContext;

    // Store cancellation token sources per connection
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();


    public DataHub(ILogger<DataHub> logger, IConfiguration configuration, FileParserService fileParserService, IHubContext<DataHub> hubContext)
    {
        _logger = logger;
        _configuration = configuration;
        _fileParserService = fileParserService;
        _hubContext = hubContext;
    }
    public async Task StartParsing(ParserOptions options)
    {
        _logger.LogInformation("Client {Context.ConnectionId} requested parsing to start with batch size {batchSize}", 
            Context.ConnectionId, options.BatchSize);
        
        try
        {
            // Validate options
            if (options.BatchSize < 1 || options.BatchSize > 10000)
            {
                await Clients.Caller.SendAsync("Error", "Batch size must be between 1 and 10000");
                return;
            }
        
            if (options.BufferSize < 1024 || options.BufferSize > 65536)
            {
                await Clients.Caller.SendAsync("Error", "Buffer size must be between 1024 and 65536");
                return;
            }
            // Cancel any existing parsing for this connection
            if (_cancellationTokens.TryGetValue(Context.ConnectionId, out var existingCts))
            {
                await existingCts.CancelAsync();
                existingCts.Dispose();
            }

            // Create new cancellation token for this operation
            var cts = new CancellationTokenSource();
            _cancellationTokens[Context.ConnectionId] = cts;

            await Clients.Caller.SendAsync("StreamStarted", cts.Token);
            
            var filePath = _configuration["DemoFile"]!;
            
            _ = ProcessFileAsync(filePath, Context.ConnectionId, cts, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private async Task ProcessFileAsync(string filePath, string connectionId, CancellationTokenSource cts, ParserOptions options)
    {
        try
        {
            await foreach (var evt in _fileParserService.ParseFileAsync(filePath, cts.Token, options))
            {
                switch (evt)
                {
                    case BatchParsedEvent batch:
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveBatch", batch.People);
                        break;                    
                    case ProgressEvent progress:
                        await _hubContext.Clients.Client(connectionId).SendAsync("Progress", 
                            progress.RecordsProcessed,
                            progress.Elapsed.TotalMilliseconds);
                        break;

                    case CompletionEvent completion:
                        await _hubContext.Clients.Client(connectionId).SendAsync("StreamCompleted", 
                            completion.TotalRecords,
                            completion.Duration.TotalMilliseconds);
                        break;

                    case CancellationEvent cancellation:
                        await _hubContext.Clients.Client(connectionId).SendAsync("StreamCancelled", cancellation.RecordsProcessedBeforeCancellation);
                        break;

                    case ErrorEvent error:
                        await _hubContext.Clients.Client(connectionId).SendAsync("ParseError", new 
                        { 
                            error.LineNumber, 
                            error.Message 
                        });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file for connection {connectionId}", connectionId);
            try
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("Error", ex.Message);
            }
            catch
            {
                // Client might have disconnected
            }
        }
        finally
        {
            // Clean up after completion or cancellation
            if (_cancellationTokens.TryRemove(connectionId, out var removedCts))
            {
                removedCts.Dispose();
            }
        }
    }

    public async Task StopParsing()
    {
        _logger.LogInformation("Client {Context.ConnectionId} requested to stop parsing", Context.ConnectionId);

        if (_cancellationTokens.TryGetValue(Context.ConnectionId, out var cts))
        {
            try
            {
                cts.Cancel();
                await Clients.Caller.SendAsync("StopRequested");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending stop notification to {Context.ConnectionId}", 
                    Context.ConnectionId);
            }
        }
        else
        {
            _logger.LogWarning("No active parsing found for connection {Context.ConnectionId}", 
                Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {Context.ConnectionId}", Context.ConnectionId);
        
        // Cancel any ongoing operations and clean up
        if (_cancellationTokens.TryRemove(Context.ConnectionId, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        await base.OnDisconnectedAsync(exception);
    }    

}
