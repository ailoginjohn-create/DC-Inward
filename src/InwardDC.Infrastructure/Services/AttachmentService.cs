using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Common;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// Attachment storage keeps files OUTSIDE the database (under the data directory)
/// and persists only metadata + a relative path. Relative paths make one-click
/// backups and restores fully portable across machines.
/// </summary>
public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly AppPaths _paths;

    public AttachmentService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, AppPaths paths)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _paths = paths;
    }

    public async Task<AttachmentDto> AttachAsync(AttachmentEntityType entityType, Guid entityId,
        string sourceFilePath, string? notes = null, CancellationToken ct = default)
    {
        if (!File.Exists(sourceFilePath))
            throw new DomainException($"File not found: {sourceFilePath}");

        var folder = _paths.AttachmentFolder(entityType.ToString(), entityId);
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(sourceFilePath);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(folder, storedName);
        File.Copy(sourceFilePath, storedPath, overwrite: true);

        var info = new FileInfo(storedPath);
        var relative = Path.GetRelativePath(_paths.DataDirectory, storedPath).Replace('\\', '/');

        var attachment = new Attachment
        {
            EntityType = entityType,
            EntityId = entityId,
            FileName = Path.GetFileName(sourceFilePath),
            StoredPath = relative,
            ContentType = MimeFromExtension(extension),
            FileSize = info.Length,
            Notes = notes ?? string.Empty,
            UploadedBy = _currentUser.UserId,
            UploadedOn = DateTime.UtcNow
        };

        await _uow.Attachments.AddAsync(attachment, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.AttachFile, entityType.ToString(), entityId,
            $"Attached file '{attachment.FileName}' to {entityType}.", ct: ct);

        return ToDto(attachment);
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetByEntityAsync(AttachmentEntityType entityType, Guid entityId, CancellationToken ct = default)
    {
        var attachments = await _uow.Attachments.GetByEntityAsync(entityType, entityId, ct);
        return attachments.Select(ToDto).ToList();
    }

    public async Task<OperationResult> DeleteAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _uow.Attachments.GetByIdAsync(attachmentId, ct);
        if (attachment is null)
            throw new NotFoundException("Attachment not found.");

        var fullPath = Path.Combine(_paths.DataDirectory, attachment.StoredPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        _uow.Attachments.Remove(attachment);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, attachment.EntityType.ToString(), attachment.EntityId,
            $"Deleted attachment '{attachment.FileName}'.", ct: ct);

        return OperationResult.Ok("Attachment deleted.");
    }

    public async Task<string?> GetFullPathAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _uow.Attachments.GetByIdAsync(attachmentId, ct);
        if (attachment is null) return null;

        var fullPath = Path.Combine(_paths.DataDirectory, attachment.StoredPath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private static AttachmentDto ToDto(Attachment a) => new()
    {
        Id = a.Id,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        FileName = a.FileName,
        StoredPath = a.StoredPath,
        ContentType = a.ContentType,
        FileSize = a.FileSize,
        Notes = a.Notes,
        UploadedOn = a.UploadedOn
    };

    private static string MimeFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xls" => "application/vnd.ms-excel",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
