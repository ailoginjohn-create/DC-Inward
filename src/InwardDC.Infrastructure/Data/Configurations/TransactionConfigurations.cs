using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InwardDC.Infrastructure.Data;

/// <summary>Configurations for the transactional tables (inward, dispatch, serials, events).</summary>
public class TransactionConfigurations :
    IEntityTypeConfiguration<InwardEntry>,
    IEntityTypeConfiguration<InwardItem>,
    IEntityTypeConfiguration<DispatchChallan>,
    IEntityTypeConfiguration<DispatchItem>,
    IEntityTypeConfiguration<SerialNumber>,
    IEntityTypeConfiguration<ItemEvent>
{
    private readonly string? _deletedFilter;

    public TransactionConfigurations(string? deletedFilter = null)
    {
        _deletedFilter = deletedFilter;
    }

    public void Configure(EntityTypeBuilder<InwardEntry> builder)
    {
        builder.ToTable("InwardEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InwardNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReferenceInvoiceNo).HasMaxLength(64);
        builder.Property(x => x.ChallanNo).HasMaxLength(64);
        builder.Property(x => x.TransportDetails).HasMaxLength(256);
        builder.Property(x => x.ReceivedBy).HasMaxLength(128);
        builder.Property(x => x.Remarks).HasMaxLength(1024);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.InwardNo);
        builder.HasIndex(x => x.InwardDate);
        builder.HasIndex(x => x.ReferenceInvoiceNo);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Purpose)
            .WithMany()
            .HasForeignKey(x => x.PurposeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.InwardEntry)
            .HasForeignKey(x => x.InwardEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<InwardItem> builder)
    {
        builder.ToTable("InwardItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ItemMake).HasMaxLength(128);
        builder.Property(x => x.ItemModel).HasMaxLength(128);
        builder.Property(x => x.HsnCode).HasMaxLength(16);
        builder.Property(x => x.Unit).HasMaxLength(32);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Rate).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.DispatchedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.Remarks).HasMaxLength(512);

        builder.HasIndex(x => x.InwardEntryId);
        builder.HasIndex(x => x.ItemId);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Serials)
            .WithOne(x => x.InwardItem)
            .HasForeignKey(x => x.InwardItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<DispatchChallan> builder)
    {
        builder.ToTable("DispatchChallans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DcNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReferenceChallanNo).HasMaxLength(64);
        builder.Property(x => x.InvoiceNo).HasMaxLength(64);
        builder.Property(x => x.TransportDetails).HasMaxLength(256);
        builder.Property(x => x.PaymentStatus).HasMaxLength(64);
        builder.Property(x => x.ModeOfDispatch).HasMaxLength(64);
        builder.Property(x => x.PodNo).HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(1024);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.DcNo);
        builder.HasIndex(x => x.DcDate);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceInwardEntry)
            .WithMany()
            .HasForeignKey(x => x.SourceInwardEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Purpose)
            .WithMany()
            .HasForeignKey(x => x.PurposeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.DispatchChallan)
            .HasForeignKey(x => x.DispatchChallanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<DispatchItem> builder)
    {
        builder.ToTable("DispatchItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ItemMake).HasMaxLength(128);
        builder.Property(x => x.ItemModel).HasMaxLength(128);
        builder.Property(x => x.HsnCode).HasMaxLength(16);
        builder.Property(x => x.Unit).HasMaxLength(32);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Rate).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(512);

        builder.HasIndex(x => x.DispatchChallanId);
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.SourceInwardItemId);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SourceInwardItem)
            .WithMany()
            .HasForeignKey(x => x.SourceInwardItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Serials)
            .WithOne(x => x.DispatchItem)
            .HasForeignKey(x => x.DispatchItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SerialNumber> builder)
    {
        builder.ToTable("SerialNumbers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SerialNo).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(512);

        var index = builder.HasIndex(x => x.SerialNo).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);

        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.InwardEntryId);
        builder.HasIndex(x => x.DispatchChallanId);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InwardEntry)
            .WithMany()
            .HasForeignKey(x => x.InwardEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DispatchChallan)
            .WithMany()
            .HasForeignKey(x => x.DispatchChallanId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ItemEvent> builder)
    {
        builder.ToTable("ItemEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SerialNo).HasMaxLength(128);
        builder.Property(x => x.ReferenceType).HasMaxLength(32);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);

        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.SerialNo);
        builder.HasIndex(x => x.EventedOn);
    }
}
