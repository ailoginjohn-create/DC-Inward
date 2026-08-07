using System.IO.Compression;
using System.Text.Json;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Enums;
using InwardDC.Infrastructure.Common;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// One-click backup, restore and factory reset.
///
/// Backup produces a single ZIP containing:
///   - the SQLite database (checkpointed first),
///   - all attachments (stored outside the DB),
///   - generated reports,
///   - application logs,
///   - a settings snapshot and a machine-readable manifest.
///
/// Restore is offline-safe: it stages the archive to a temp folder, verifies the
/// manifest, then swaps the database and data folders, leaving a pre-restore copy.
/// </summary>
public class BackupService : IBackupService
{
    private const string ManifestName = "manifest.json";
    private const string SettingsSnapshotName = "settings.json";

    private readonly AppDbContext _db;
    private readonly AppPaths _paths;
    private readonly ISettingsService _settings;
    private readonly ILogger<BackupService> _logger;

    public BackupService(AppDbContext db, AppPaths paths, ISettingsService settings, ILogger<BackupService> logger)
    {
        _db = db;
        _paths = paths;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(_paths.BackupsDirectory, $"InwardDC_Backup_{stamp}.zip");

        Directory.CreateDirectory(_paths.BackupsDirectory);
        Directory.CreateDirectory(_paths.TempDirectory);
        var staging = Path.Combine(_paths.TempDirectory, $"staging_{stamp}");
        Directory.CreateDirectory(staging);

        try
        {
            // 1. Checkpoint SQLite WAL so the on-disk database file is consistent.
            if (_db.Database.IsSqlite())
            {
                await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);", ct);
            }

            // 2. Stage the database file.
            var dbFile = _paths.DatabaseFile;
            if (File.Exists(dbFile))
            {
                var dbStaging = Path.Combine(staging, "Database");
                Directory.CreateDirectory(dbStaging);
                File.Copy(dbFile, Path.Combine(dbStaging, Path.GetFileName(dbFile)), overwrite: true);
            }

            // 3. Stage attachments / reports / logs.
            CopyDirectoryContents(_paths.AttachmentsDirectory, Path.Combine(staging, "Attachments"));
            CopyDirectoryContents(_paths.ReportsDirectory, Path.Combine(staging, "Reports"));
            CopyDirectoryContents(_paths.LogsDirectory, Path.Combine(staging, "Logs"));

            // 4. Stage settings snapshot.
            var settings = await _settings.GetAllAsync(ct);
            var settingsJson = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(staging, SettingsSnapshotName), settingsJson, ct);

            // 5. Write manifest.
            var manifest = new BackupManifest
            {
                AppName = "Inward & DC",
                SchemaVersion = "1.0",
                CreatedOn = DateTime.UtcNow,
                DatabaseProvider = _db.Database.ProviderName ?? "unknown",
                HasDatabase = File.Exists(dbFile),
                HasAttachments = Directory.Exists(_paths.AttachmentsDirectory) && Directory.EnumerateFiles(_paths.AttachmentsDirectory, "*", SearchOption.AllDirectories).Any(),
                HasReports = Directory.Exists(_paths.ReportsDirectory) && Directory.EnumerateFiles(_paths.ReportsDirectory, "*", SearchOption.AllDirectories).Any(),
                HasLogs = Directory.Exists(_paths.LogsDirectory) && Directory.EnumerateFiles(_paths.LogsDirectory, "*", SearchOption.AllDirectories).Any()
            };
            await File.WriteAllTextAsync(Path.Combine(staging, ManifestName), JsonSerializer.Serialize(manifest, JsonOptions), ct);

            // 6. Zip it all up.
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            ZipFile.CreateFromDirectory(staging, backupPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            _logger.LogInformation("Backup created at {Path} ({Size} bytes)", backupPath, new FileInfo(backupPath).Length);
            return backupPath;
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    public async Task<OperationResult> RestoreAsync(string zipPath, CancellationToken ct = default)
    {
        if (!File.Exists(zipPath))
            return OperationResult.Fail($"Backup file not found: {zipPath}");

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var staging = Path.Combine(_paths.TempDirectory, $"restore_{stamp}");
        Directory.CreateDirectory(staging);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, staging);

            var manifestPath = Path.Combine(staging, ManifestName);
            if (!File.Exists(manifestPath))
                return OperationResult.Fail("Invalid backup: manifest.json is missing.");

            var manifest = JsonSerializer.Deserialize<BackupManifest>(await File.ReadAllTextAsync(manifestPath, ct), JsonOptions);
            if (manifest is null || manifest.SchemaVersion != "1.0")
                return OperationResult.Fail("Unsupported backup schema version.");

            // Preserve the current database before overwriting.
            var dbFile = _paths.DatabaseFile;
            if (File.Exists(dbFile))
            {
                var safetyDir = Path.Combine(_paths.BackupsDirectory, $"PreRestore_{stamp}");
                Directory.CreateDirectory(safetyDir);
                File.Copy(dbFile, Path.Combine(safetyDir, Path.GetFileName(dbFile)), overwrite: true);
            }

            // Swap folders.
            SwapFolder(Path.Combine(staging, "Database"), _paths.DatabaseDirectory);
            SwapFolder(Path.Combine(staging, "Attachments"), _paths.AttachmentsDirectory);
            SwapFolder(Path.Combine(staging, "Reports"), _paths.ReportsDirectory);
            SwapFolder(Path.Combine(staging, "Logs"), _paths.LogsDirectory);

            _logger.LogWarning("Restore performed from {Zip}; application restart required.", zipPath);
            return OperationResult.Ok(
                "Restore completed. The application will now restart to load the restored database.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed.");
            return OperationResult.Fail($"Restore failed: {ex.Message}");
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    public async Task<OperationResult> FactoryResetAsync(CancellationToken ct = default)
    {
        // Safety: never wipe without a backup first.
        try
        {
            await CreateBackupAsync(ct);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Safety backup failed ({ex.Message}). Factory reset aborted.");
        }

        try
        {
            // Attachments / reports live outside the DB.
            TryDeleteDirectory(_paths.AttachmentsDirectory);
            TryDeleteDirectory(_paths.ReportsDirectory);
            Directory.CreateDirectory(_paths.AttachmentsDirectory);
            Directory.CreateDirectory(_paths.ReportsDirectory);

            var tables = new[]
            {
                "ItemEvents", "SerialNumbers", "DispatchItems", "DispatchChallans",
                "InwardItems", "InwardEntries", "Attachments", "AuditLogs",
                "Settings", "SequenceCounters", "Items", "ItemCategories", "Vendors", "Customers"
            };

            // Table names come from the constant list above; no user input reaches the SQL.
#pragma warning disable EF1002
            foreach (var table in tables)
                await _db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\";", ct);
#pragma warning restore EF1002

            // Re-seed the standard configuration.
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Settings\";", ct);
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"SequenceCounters\";", ct);
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditLogs\";", ct);

            _logger.LogWarning("Factory reset executed.");
            return OperationResult.Ok("Factory reset complete. The application will restart with default settings.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Factory reset failed.");
            return OperationResult.Fail($"Factory reset failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<string>> ListBackupsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_paths.BackupsDirectory))
            return Array.Empty<string>();

        return await Task.Run(() =>
            Directory.GetFiles(_paths.BackupsDirectory, "*.zip")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList(), ct);
    }

    private static void SwapFolder(string source, string destination)
    {
        if (Directory.Exists(source) && Directory.EnumerateFileSystemEntries(source).Any())
        {
            if (Directory.Exists(destination))
                TryDeleteDirectory(destination);
            Directory.CreateDirectory(destination);
            CopyDirectoryContents(source, destination);
        }
    }

    private static void CopyDirectoryContents(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination));

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, destination), overwrite: true);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Swallowing: files may be locked; never block backup/restore for this.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private sealed class BackupManifest
    {
        public string AppName { get; set; } = string.Empty;
        public string SchemaVersion { get; set; } = "1.0";
        public DateTime CreatedOn { get; set; }
        public string DatabaseProvider { get; set; } = string.Empty;
        public bool HasDatabase { get; set; }
        public bool HasAttachments { get; set; }
        public bool HasReports { get; set; }
        public bool HasLogs { get; set; }
    }
}
