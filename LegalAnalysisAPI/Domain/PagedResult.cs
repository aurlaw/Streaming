namespace LegalAnalysisAPI.Domain;

/// <summary>
/// Represents a paginated result set.
/// </summary>
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int? NextId { get; init; }
    public bool HasMore { get; init; }
}