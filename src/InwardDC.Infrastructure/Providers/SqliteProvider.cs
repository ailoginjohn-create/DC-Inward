using InwardDC.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Providers;

/// <summary>SQLite provider. Single-PC offline default.</summary>
public class SqliteProvider : IDatabaseProvider
{
    public DatabaseProviderKind Kind => DatabaseProviderKind.SQLite;
    public string DisplayName => "SQLite";
    public string DefaultConnectionString => "Data Source={dbFile}";
    public bool CanCreateDatabase => true;

    public string GetConnectionString(string dataDirectory, string? overrideConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(overrideConnectionString))
            return overrideConnectionString;

        var dbFile = Path.Combine(dataDirectory, "InwardDC.db");
        return $"Data Source={dbFile}";
    }

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationsAssembly)
    {
        options.UseSqlite(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
    }
}
