namespace FileStreamDemo.Data.Models;

public abstract class ParserEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BatchParsedEvent : ParserEvent
{
    public List<Person> People { get; set; } = new();
    public int TotalProcessedSoFar { get; set; }
}

public class ProgressEvent : ParserEvent
{
    public int RecordsProcessed { get; set; }
    public double PercentComplete { get; set; }
    public TimeSpan Elapsed { get; set; }
}

public class CompletionEvent : ParserEvent
{
    public int TotalRecords { get; set; }
    public TimeSpan Duration { get; set; }
    public int ErrorCount { get; set; }
}

public class ErrorEvent : ParserEvent
{
    public required string Message { get; set; }
    public int LineNumber { get; set; }
    public required Exception Exception { get; set; }
}

public class CancellationEvent : ParserEvent
{
    public int RecordsProcessedBeforeCancellation { get; set; }
    public TimeSpan Duration { get; set; }
}