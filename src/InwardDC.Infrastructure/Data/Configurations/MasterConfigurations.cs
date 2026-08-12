using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InwardDC.Infrastructure.Data;

/// <summary>Configurations for the master tables (customers, vendors, items, categories, purposes).</summary>
public class MasterConfigurations : IEntityTypeConfiguration<Customer>,
    IEntityTypeConfiguration<Vendor>,
    IEntityTypeConfiguration<Item>,
    IEntityTypeConfiguration<ItemCategory>,
    IEntityTypeConfiguration<Purpose>
{
    private readonly string? _deletedFilter;

    public MasterConfigurations(string? deletedFilter)
    {
        _deletedFilter = deletedFilter;
    }

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(128);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Mobile).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(128);
        builder.Property(x => x.AddressLine1).HasMaxLength(256);
        builder.Property(x => x.AddressLine2).HasMaxLength(256);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.State).HasMaxLength(128);
        builder.Property(x => x.Pincode).HasMaxLength(16);
        builder.Property(x => x.Country).HasMaxLength(64);
        builder.Property(x => x.GSTIN).HasMaxLength(32);
        builder.Property(x => x.PAN).HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(1024);

        var index = builder.HasIndex(x => x.Code).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
        builder.HasIndex(x => x.Name);
    }

    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(128);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Mobile).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(128);
        builder.Property(x => x.AddressLine1).HasMaxLength(256);
        builder.Property(x => x.AddressLine2).HasMaxLength(256);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.State).HasMaxLength(128);
        builder.Property(x => x.Pincode).HasMaxLength(16);
        builder.Property(x => x.Country).HasMaxLength(64);
        builder.Property(x => x.GSTIN).HasMaxLength(32);
        builder.Property(x => x.PAN).HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(1024);

        var index = builder.HasIndex(x => x.Code).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
        builder.HasIndex(x => x.Name);
    }

    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Make).HasMaxLength(128);
        builder.Property(x => x.Model).HasMaxLength(128);
        builder.Property(x => x.Unit).HasMaxLength(32);
        builder.Property(x => x.HsnCode).HasMaxLength(16);
        builder.Property(x => x.Description).HasMaxLength(1024);

        var index = builder.HasIndex(x => x.Code).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => new { x.Make, x.Model });

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ToTable("ItemCategories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);

        var index = builder.HasIndex(x => x.Code).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
    }

    public void Configure(EntityTypeBuilder<Purpose> builder)
    {
        builder.ToTable("Purposes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);

        var index = builder.HasIndex(x => x.Name).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);
    }
}
