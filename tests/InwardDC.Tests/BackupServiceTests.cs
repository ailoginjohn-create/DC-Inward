using Xunit;
using System.IO.Compression;
using InwardDC.Application.Services;
using InwardDC.Domain.Entities;
using InwardDC.Infrastructure.Common;
using InwardDC.Infrastructure.Data;
using InwardDC.Infrastructure.Repositories;
using InwardDC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class BackupServiceTests
{
    private sealed class BackupHarness : IDisposable
    {
        public AppPaths Paths { get; }
        public string RootDir { get; }

        public BackupHarness()
        {
            RootDir = Path.Combine(Path.GetTempPath(), "inwarddc-backup-tests", Guid.NewGuid().ToString("N"));
            Paths = new AppPaths(RootDir);
        }

        public AppDbContext OpenDb()
        {
            // Pooling=False so a file swap is picked up by subsequent connections
            // (a pooled handle would keep reading the deleted pre-restore inode).
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Paths.DatabaseFile};Pooling=False",
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name!))
                .Options;
            return new AppDbContext(options);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootDir))
                    Directory.Delete(RootDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public async Task CreateBackupAsync_ZipsManifestSettingsAndDatabase()
    {
        using var h = new BackupHarness();
        using (var db = h.OpenDb())
        {
            db.Database.Migrate();
            var uow = new UnitOfWork(db);
            await new SeedService(uow, NullLogger<SeedService>.Instance).SeedAsync();
        }

        using (var db = h.OpenDb())
        {
            var uow = new UnitOfWork(db);
            var settings = new SettingsService(uow, new TestCurrentUser());
            var backup = new BackupService(db, h.Paths, settings, NullLogger<BackupService>.Instance);

            var zipPath = await backup.CreateBackupAsync();

            Assert.True(File.Exists(zipPath));
            using var archive = ZipFile.OpenRead(zipPath);
            var names = archive.Entries.Select(e => e.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Contains("manifest.json", names);
            Assert.Contains("settings.json", names);
            Assert.Contains("Database/InwardDC.db", names);

            var manifestEntry = archive.GetEntry("manifest.json");
            using var reader = new StreamReader(manifestEntry!.Open());
            var json = await reader.ReadToEndAsync();
            Assert.Contains("\"SchemaVersion\"", json);
            Assert.Contains("\"1.0\"", json);
        }
    }

    [Fact]
    public async Task RestoreAsync_ReplacesDatabaseWithBackupContent()
    {
        using var h = new BackupHarness();

        // Phase 1: create + seed, then add a record and back up.
        Guid customerId;
        string backupPath;
        using (var db = h.OpenDb())
        {
            db.Database.Migrate();
            var uow = new UnitOfWork(db);
            await new SeedService(uow, NullLogger<SeedService>.Instance).SeedAsync();

            await uow.Customers.AddAsync(new Customer { Code = "CUS-999", Name = "Pre Backup Customer", IsActive = true });
            await uow.SaveChangesAsync();
            customerId = (await uow.Customers.GetByCodeAsync("CUS-999"))!.Id;

            var backup = new BackupService(db, h.Paths, new SettingsService(uow, new TestCurrentUser()), NullLogger<BackupService>.Instance);
            backupPath = await backup.CreateBackupAsync();
        }

        // Phase 2: add another record (simulating drift), then restore.
        using (var db = h.OpenDb())
        {
            var uow = new UnitOfWork(db);
            await uow.Customers.AddAsync(new Customer { Code = "CUS-777", Name = "After Backup Customer", IsActive = true });
            await uow.SaveChangesAsync();

            var backup = new BackupService(db, h.Paths, new SettingsService(uow, new TestCurrentUser()), NullLogger<BackupService>.Instance);
            var restore = await backup.RestoreAsync(backupPath);
            Assert.True(restore.Success);
        }

        // Phase 3: the drifted record must be gone, the pre-backup record present.
        using (var db = h.OpenDb())
        {
            var uow = new UnitOfWork(db);
            Assert.NotNull(await uow.Customers.GetByIdAsync(customerId));
            Assert.Null(await uow.Customers.GetByCodeAsync("CUS-777"));
        }
    }

    [Fact]
    public async Task RestoreAsync_FailsGracefully_WhenManifestMissing()
    {
        using var h = new BackupHarness();
        using (var db = h.OpenDb())
        {
            db.Database.Migrate();
            var uow = new UnitOfWork(db);
            var backup = new BackupService(db, h.Paths, new SettingsService(uow, new TestCurrentUser()), NullLogger<BackupService>.Instance);

            var badZip = Path.Combine(h.Paths.BackupsDirectory, "bad.zip");
            using (var archive = ZipFile.Open(badZip, ZipArchiveMode.Create))
            {
                archive.CreateEntry("random.txt");
            }

            var result = await backup.RestoreAsync(badZip);
            Assert.False(result.Success);
            Assert.Contains("manifest.json", result.Message);
        }
    }
}
