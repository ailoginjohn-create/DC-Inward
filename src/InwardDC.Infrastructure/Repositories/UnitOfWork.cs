using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

/// <summary>
/// Unit of work bound to the EF Core DbContext. Every repository is created once
/// and reused. Transactions are exposed so cross-aggregate operations stay atomic.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public IUserRepository Users { get; }
    public ICustomerRepository Customers { get; }
    public IVendorRepository Vendors { get; }
    public IItemRepository Items { get; }
    public IItemCategoryRepository ItemCategories { get; }
    public IInwardRepository Inwards { get; }
    public IDCRepository DCs { get; }
    public IAttachmentRepository Attachments { get; }
    public IAuditLogRepository AuditLogs { get; }
    public ISettingRepository Settings { get; }
    public ISequenceRepository Sequences { get; }
    public IItemEventRepository ItemEvents { get; }
    public ISerialNumberRepository SerialNumbers { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Users = new UserRepository(db);
        Customers = new CustomerRepository(db);
        Vendors = new VendorRepository(db);
        Items = new ItemRepository(db);
        ItemCategories = new ItemCategoryRepository(db);
        Inwards = new InwardRepository(db);
        DCs = new DCRepository(db);
        Attachments = new AttachmentRepository(db);
        AuditLogs = new AuditLogRepository(db);
        Settings = new SettingRepository(db);
        Sequences = new SequenceRepository(db);
        ItemEvents = new ItemEventRepository(db);
        SerialNumbers = new SerialNumberRepository(db);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await action();
        await transaction.CommitAsync(ct);
    }
}
