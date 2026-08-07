using InwardDC.Application.Common;
using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Data;

/// <summary>
/// Application database context. Kept deliberately thin — all table/relationship
/// shape lives in the IEntityTypeConfiguration classes so each provider produces
/// an identical, portable schema.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUser;

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<InwardEntry> InwardEntries => Set<InwardEntry>();
    public DbSet<InwardItem> InwardItems => Set<InwardItem>();
    public DbSet<DispatchChallan> DispatchChallans => Set<DispatchChallan>();
    public DbSet<DispatchItem> DispatchItems => Set<DispatchItem>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<ItemEvent> ItemEvents => Set<ItemEvent>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedOn = now;
                    entry.Entity.ModifiedBy = userId;
                    break;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Filtered unique indexes keep "reuse code after soft delete" working. The SQL
        // predicate differs per provider (and MySQL lacks partial indexes entirely).
        var deletedFilter = Database.IsSqlite() ? "IsDeleted = 0"
            : Database.IsNpgsql() ? "\"IsDeleted\" = false"
            : Database.IsSqlServer() ? "[IsDeleted] = 0"
            : null;

        builder.ApplyConfiguration(new UserConfiguration(deletedFilter));
        builder.ApplyConfiguration<Customer>(new MasterConfigurations(deletedFilter));
        builder.ApplyConfiguration<Vendor>(new MasterConfigurations(deletedFilter));
        builder.ApplyConfiguration<Item>(new MasterConfigurations(deletedFilter));
        builder.ApplyConfiguration<ItemCategory>(new MasterConfigurations(deletedFilter));
        builder.ApplyConfiguration<InwardEntry>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<InwardItem>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<DispatchChallan>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<DispatchItem>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<SerialNumber>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<ItemEvent>(new TransactionConfigurations(deletedFilter));
        builder.ApplyConfiguration<Attachment>(new SupportConfigurations(deletedFilter));
        builder.ApplyConfiguration<AuditLog>(new SupportConfigurations(deletedFilter));
        builder.ApplyConfiguration<Setting>(new SupportConfigurations(deletedFilter));
        builder.ApplyConfiguration<SequenceCounter>(new SupportConfigurations(deletedFilter));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
