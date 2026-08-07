using InwardDC.Domain.Enums;

namespace InwardDC.Application.DTOs;

/// <summary>One hit from the global search screen.</summary>
public class SearchHitDto
{
    public string Module { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
}

/// <summary>Aggregated global search response grouped by module.</summary>
public class SearchResultDto
{
    public IReadOnlyList<SearchHitDto> Customers { get; set; } = Array.Empty<SearchHitDto>();
    public IReadOnlyList<SearchHitDto> Items { get; set; } = Array.Empty<SearchHitDto>();
    public IReadOnlyList<SearchHitDto> Inwards { get; set; } = Array.Empty<SearchHitDto>();
    public IReadOnlyList<SearchHitDto> Dispatches { get; set; } = Array.Empty<SearchHitDto>();
    public int Total => Customers.Count + Items.Count + Inwards.Count + Dispatches.Count;
}

/// <summary>One row of an item history / timeline.</summary>
public class ItemHistoryDto
{
    public DateTime EventedOn { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

/// <summary>Current stock position of a serial tracked item.</summary>
public class ItemStockDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public SerialStatus Status { get; set; }
    public string InwardNo { get; set; } = string.Empty;
    public string DcNo { get; set; } = string.Empty;
    public DateTime? DispatchedOn { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
