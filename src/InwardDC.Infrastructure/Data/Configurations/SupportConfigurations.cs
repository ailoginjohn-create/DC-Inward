using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InwardDC.Infrastructure.Data;

/// <summary>Configurations for the support tables (attachments, audit logs, settings, sequences).</summary>
public class SupportConfigurations :
    IEntityTypeConfiguration<Attachment>,
    IEntityTypeConfiguration<AuditLog>,
    IEntityTypeConfiguration<Setting>,
    IEntityTypeConfiguration<SequenceCounter>
{
    private readonly string? _deletedFilter;

    public SupportConfigurations(string? deletedFilter)
    {
        _deletedFilter = deletedFilter;
    }

    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StoredPath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128);
        builder.Property(x => x.Notes).HasMaxLength(512);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(64);
        builder.Property(x => x.FullName).HasMaxLength(128);
        builder.Property(x => x.EntityType).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Details).HasMaxLength(8192);
        builder.Property(x => x.IpAddress).HasMaxLength(64);

        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Action);
    }

    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(4096);
        builder.Property(x => x.Group).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.DataType).HasMaxLength(16);

        var index = builder.HasIndex(x => x.Key).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
    }

    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("SequenceCounters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityName).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(16);

        var index = builder.HasIndex(x => new { x.EntityName, x.Year }).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
    }
}
