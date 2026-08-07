namespace InwardDC.Infrastructure.Common;

/// <summary>
/// Central location for all application data paths. Everything (database,
/// attachments, reports, backups, logs) lives under one data directory so the
/// one-click backup can collect it all and uninstalling is clean.
/// </summary>
public class AppPaths
{
    public string DataDirectory { get; }
    public string DatabaseDirectory => Path.Combine(DataDirectory, "Database");
    public string DatabaseFile => Path.Combine(DatabaseDirectory, "InwardDC.db");
    public string AttachmentsDirectory => Path.Combine(DataDirectory, "Attachments");
    public string ReportsDirectory => Path.Combine(DataDirectory, "Reports");
    public string BackupsDirectory => Path.Combine(DataDirectory, "Backups");
    public string LogsDirectory => Path.Combine(DataDirectory, "Logs");
    public string TempDirectory => Path.Combine(DataDirectory, "Temp");

    public AppPaths(string? dataDirectory = null)
    {
        DataDirectory = ResolveDataDirectory(dataDirectory);
        EnsureDirectories();
    }

    private static string ResolveDataDirectory(string? dataDirectory)
    {
        if (!string.IsNullOrWhiteSpace(dataDirectory))
            return dataDirectory;

        var envOverride = Environment.GetEnvironmentVariable("INWARDDC_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(envOverride))
            return envOverride;

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "InwardDC");
    }

    private void EnsureDirectories()
    {
        foreach (var dir in new[]
        {
            DataDirectory, DatabaseDirectory, AttachmentsDirectory,
            ReportsDirectory, BackupsDirectory, LogsDirectory, TempDirectory
        })
        {
            Directory.CreateDirectory(dir);
        }
    }

    public string AttachmentFolder(string entityType, Guid entityId)
        => Path.Combine(AttachmentsDirectory, entityType, entityId.ToString("N"));
}
