using System.Collections.Concurrent;
using FileStreamDemo.Services;
using Microsoft.AspNetCore.SignalR;

namespace FileStreamDemo.Hubs;

public class DataHub : Hub
{
    private readonly ILogger<DataHub> _logger;
    private readonly FileParserService _fileParserService;
    private readonly IConfiguration _configuration;

    // Store cancellation token sources per connection
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();


    public DataHub(ILogger<DataHub> logger, FileParserService fileParserService, IConfiguration configuration)
    {
        _logger = logger;
        _fileParserService = fileParserService;
        _configuration = configuration;
    }
    
    // You can add connection lifecycle logging if you want
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {Context.ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public async Task StartParsing(int batchSize = 100)
    {
        _logger.LogInformation("Client {Context.ConnectionId} requested parsing to start with batch size {batchSize}", 
            Context.ConnectionId, batchSize);
        
        try
        {
            // Cancel any existing parsing for this connection
            if (_cancellationTokens.TryGetValue(Context.ConnectionId, out var existingCts))
            {
                await existingCts.CancelAsync();
                existingCts.Dispose();
            }

            // Create new cancellation token for this operation
            var cts = new CancellationTokenSource();
            _cancellationTokens[Context.ConnectionId] = cts;

            await Clients.Caller.SendAsync("StreamStarted");
            
            var filePath = _configuration["DemoFile"]!;
            _ = ProcessFileAsync(filePath, Context.ConnectionId, batchSize, cts);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task StopParsing()
    {
        _logger.LogInformation("Client {Context.ConnectionId} requested to stop parsing", Context.ConnectionId);

        if (_cancellationTokens.TryGetValue(Context.ConnectionId, out var cts))
        {
            try
            {
                await cts.CancelAsync();
                await Clients.Caller.SendAsync("StopRequested");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending stop notification to {Context.ConnectionId}", 
                    Context.ConnectionId);
                // Don't rethrow - the cancellation still happened
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
        _logger.LogInformation("Client disconnected: {Context.ConnectionId}",Context.ConnectionId);
        
        // Cancel any ongoing operations and clean up
        if (_cancellationTokens.TryRemove(Context.ConnectionId, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task ProcessFileAsync(string filePath, string connectionId, int batchSize, CancellationTokenSource cts)
    {
        try
        {
            await _fileParserService.ParseAndStreamFileAsync(filePath, connectionId, cts.Token, batchSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing file for connection {connectionId}");
            try
            {
                await Clients.Client(connectionId).SendAsync("Error", ex.Message);
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
    
}
