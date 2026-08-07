using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Every meaningful action is recorded here and is
/// used by the audit log screen. Details holds a JSON snapshot of the change.
/// </summary>
public class AuditLog : EntityBase
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
