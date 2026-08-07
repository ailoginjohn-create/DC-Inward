using InwardDC.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Providers;

/// <summary>
/// PostgreSQL provider. Used when the application is deployed multi-user / online.
/// Connection string is supplied explicitly via configuration.
/// </summary>
public class PostgreSqlProvider : IDatabaseProvider
{
    public DatabaseProviderKind Kind => DatabaseProviderKind.PostgreSQL;
    public string DisplayName => "PostgreSQL";
    public string DefaultConnectionString => "Host=localhost;Port=5432;Database=inwarddc;Username=postgres;Password=postgres";
    public bool CanCreateDatabase => true;

    public string GetConnectionString(string dataDirectory, string? overrideConnectionString)
        => string.IsNullOrWhiteSpace(overrideConnectionString) ? DefaultConnectionString : overrideConnectionString;

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationsAssembly)
    {
        options.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
    }
}

/// <summary>Microsoft SQL Server provider.</summary>
public class SqlServerProvider : IDatabaseProvider
{
    public DatabaseProviderKind Kind => DatabaseProviderKind.SqlServer;
    public string DisplayName => "SQL Server";
    public string DefaultConnectionString => "Server=localhost;Database=inwarddc;User Id=sa;Password=Your_password123;TrustServerCertificate=True";
    public bool CanCreateDatabase => true;

    public string GetConnectionString(string dataDirectory, string? overrideConnectionString)
        => string.IsNullOrWhiteSpace(overrideConnectionString) ? DefaultConnectionString : overrideConnectionString;

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationsAssembly)
    {
        options.UseSqlServer(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
    }
}

/// <summary>MySQL / MariaDB provider (Pomelo).</summary>
public class MySqlProvider : IDatabaseProvider
{
    public DatabaseProviderKind Kind => DatabaseProviderKind.MySQL;
    public string DisplayName => "MySQL";
    public string DefaultConnectionString => "Server=localhost;Database=inwarddc;User=root;Password=root";
    public bool CanCreateDatabase => true;

    public string GetConnectionString(string dataDirectory, string? overrideConnectionString)
        => string.IsNullOrWhiteSpace(overrideConnectionString) ? DefaultConnectionString : overrideConnectionString;

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationsAssembly)
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
            b => b.MigrationsAssembly(migrationsAssembly));
    }
}
