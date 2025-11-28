using System.Text;
using FileStreamDemo.Hubs;
using FileStreamDemo.Models;
using Microsoft.AspNetCore.SignalR;

namespace FileStreamDemo.Services;

public class FileParserService
{
    private readonly IHubContext<DataHub> _hubContext;
    private readonly ILogger<FileParserService> _logger;

    public FileParserService(IHubContext<DataHub> hubContext, ILogger<FileParserService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }
    
    public async Task ParseAndStreamFileAsync(string filePath, string connectionId, CancellationToken cancellationToken, int batchSize = 100)
       {
           if (!File.Exists(filePath))
           {
               await _hubContext.Clients.Client(connectionId)
                   .SendAsync("Error", "File not found", cancellationToken);
               return;
           }
           var totalProcessed = 0;
           var lineNumber = 0;
           
           try
           {
               await _hubContext.Clients.Client(connectionId)
                   .SendAsync("StreamStarted", cancellationToken);
               
               
               // Read the entire file into Memory<byte> (async-friendly)
               var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
               Memory<byte> memory = fileBytes;
                        
               var batch = new List<Person>(batchSize);

               // Process the file synchronously using spans
               // This extracts all the people without any awaits
               var allPeople = ParseAllPeople(memory.Span, ref lineNumber);

               // Now stream the results to the client with awaits
               foreach (var person in allPeople)
               {
                   // Check for cancellation
                   cancellationToken.ThrowIfCancellationRequested();
                   
                   batch.Add(person);
                   totalProcessed++;

                   // Send batch when full
                   if (batch.Count >= batchSize)
                   {
                       await _hubContext.Clients.Client(connectionId)
                           .SendAsync("ReceiveBatch", batch.ToArray(), cancellationToken);
                                
                       await _hubContext.Clients.Client(connectionId)
                           .SendAsync("Progress", totalProcessed, cancellationToken);

                       batch.Clear();
                                
                       // Small delay to prevent overwhelming the client
                       await Task.Delay(10, cancellationToken);
                   }
               }

               // Send remaining batch
               if (batch.Count > 0)
               {
                   await _hubContext.Clients.Client(connectionId)
                       .SendAsync("ReceiveBatch", batch.ToArray(), cancellationToken);
               }

               // Send completion
               await _hubContext.Clients.Client(connectionId)
                   .SendAsync("StreamCompleted", totalProcessed, cancellationToken);

               _logger.LogInformation("Completed streaming {totalProcessed} records", totalProcessed);
           }
           catch (OperationCanceledException)
           {
               _logger.LogInformation("Parsing cancelled by client {connectionId}", connectionId);
               await _hubContext.Clients.Client(connectionId)
                   .SendAsync("StreamCancelled", totalProcessed, cancellationToken);
           }           
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error during file parsing and streaming");
           }           
       }
    
    private List<Person> ParseAllPeople(ReadOnlySpan<byte> fileSpan, ref int lineNumber)
    {
        var people = new List<Person>();

        while (fileSpan.Length > 0)
        {
            var newlineIndex = fileSpan.IndexOf((byte)'\n');
            ReadOnlySpan<byte> line;

            if (newlineIndex >= 0)
            {
                line = fileSpan.Slice(0, newlineIndex);
                fileSpan = fileSpan.Slice(newlineIndex + 1);
            }
            else
            {
                line = fileSpan;
                fileSpan = ReadOnlySpan<byte>.Empty;
            }

            // Handle Windows line endings (\r\n)
            if (line.Length > 0 && line[line.Length - 1] == (byte)'\r')
            {
                line = line.Slice(0, line.Length - 1);
            }

            if (line.Length == 0) continue;

            lineNumber++;

            try
            {
                var person = ParsePersonFromLine(line);
                people.Add(person);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error parsing line {lineNumber}: {ex.Message}", lineNumber, ex.Message);
            }
        }

        return people;
    }
    
    private Person ParsePersonFromLine(ReadOnlySpan<byte> line)
    {
        // Find first comma
        var firstComma = line.IndexOf((byte)',');
        if (firstComma < 0) throw new FormatException("Invalid line format");
   
        // Extract first name
        var firstNameBytes = line.Slice(0, firstComma);
        var firstName = Encoding.UTF8.GetString(firstNameBytes);
   
        // Move past first comma
        line = line.Slice(firstComma + 1);
   
        // Find second comma
        int secondComma = line.IndexOf((byte)',');
        if (secondComma < 0) throw new FormatException("Invalid line format");
   
        // Extract last name
        var lastNameBytes = line.Slice(0, secondComma);
        string lastName = Encoding.UTF8.GetString(lastNameBytes);
   
        // Extract date (remaining part)
        var dateBytes = line.Slice(secondComma + 1);
        var dateString = Encoding.UTF8.GetString(dateBytes);
   
        if (!DateOnly.TryParse(dateString, out DateOnly birthDate))
            throw new FormatException("Invalid date format");
   
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate
        };
    }    
}
