namespace FileStreamDemo.Data.Models;

public class ParserOptions
{
    /// <summary>
    /// Minimum time interval between progress events (milliseconds)
    /// </summary>
    public int ProgressIntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// Minimum number of records between progress events
    /// </summary>
    public int ProgressRecordInterval { get; set; } = 5000;
    
    /// <summary>
    /// Number of records to accumulate before yielding a batch event
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Whether to yield batch events containing parsed records
    /// </summary>
    public bool YieldBatchEvents { get; set; } = true;
    
    /// <summary>
    /// Whether to yield progress events
    /// </summary>
    public bool YieldProgressEvents { get; set; } = true;
    
    /// <summary>
    /// Size of the read buffer in bytes
    /// </summary>
    public int BufferSize { get; set; } = 8192; // 8KB    
}