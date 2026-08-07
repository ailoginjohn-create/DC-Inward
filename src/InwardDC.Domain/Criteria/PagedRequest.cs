namespace InwardDC.Domain.Criteria;

/// <summary>
/// Generic paged request. Kept in the Domain layer so repository implementations
/// (EF Core today, REST API tomorrow) can translate it into their own query format.
/// </summary>
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = string.Empty;
    public bool SortDescending { get; set; }
}

/// <summary>Generic paged result returned by every paged repository method.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
