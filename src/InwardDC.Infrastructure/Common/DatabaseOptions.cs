using InwardDC.Domain.Enums;

namespace InwardDC.Infrastructure.Common;

/// <summary>Database connection settings resolved from appsettings.json.</summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";
    public DatabaseProviderKind Provider { get; set; } = DatabaseProviderKind.SQLite;
    public string? ConnectionString { get; set; }
    public string ProviderName => Provider.ToString();
}
