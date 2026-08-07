using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Attachment metadata. Files are stored on disk (outside the database) and only the
/// path + metadata is persisted — keeps the DB small and backup friendly.
/// </summary>
public class Attachment : EntityBase
{
    public AttachmentEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
}
