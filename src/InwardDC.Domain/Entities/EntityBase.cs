namespace InwardDC.Domain.Entities;

/// <summary>
/// Base entity that every aggregate in the system inherits from.
/// Provides the audit trail fields and soft-delete support required by the
/// enterprise architecture (GUID keys + soft delete + full audit columns).
/// </summary>
public abstract class EntityBase
{
    /// <summary>GUID primary key (provider independent, supports distributed/REST future).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Soft-delete flag. Hard deletes are never performed on master data.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>When the record was soft-deleted.</summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>Who soft-deleted the record.</summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>Created timestamp (UTC).</summary>
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    /// <summary>Who created the record.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Last modified timestamp (UTC).</summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>Who last modified the record.</summary>
    public Guid? ModifiedBy { get; set; }
}
