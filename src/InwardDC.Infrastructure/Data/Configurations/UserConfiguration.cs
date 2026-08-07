using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InwardDC.Infrastructure.Data;

/// <summary>Configuration for the Users table.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly string? _deletedFilter;

    public UserConfiguration(string? deletedFilter)
    {
        _deletedFilter = deletedFilter;
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(128);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();

        var index = builder.HasIndex(x => x.UserName).IsUnique();
        if (_deletedFilter is not null)
            index.HasFilter(_deletedFilter);

        builder.HasIndex(x => new { x.Role, x.IsActive });
    }
}
