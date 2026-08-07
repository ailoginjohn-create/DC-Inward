using InwardDC.Domain.Enums;

namespace InwardDC.Infrastructure.Providers;

/// <summary>Builds provider implementations from the configured enum kind.</summary>
public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(DatabaseProviderKind kind) => kind switch
    {
        DatabaseProviderKind.PostgreSQL => new PostgreSqlProvider(),
        DatabaseProviderKind.SqlServer => new SqlServerProvider(),
        DatabaseProviderKind.MySQL => new MySqlProvider(),
        _ => new SqliteProvider()
    };

    public static IDatabaseProvider Create(string providerName)
    {
        return Enum.TryParse<DatabaseProviderKind>(providerName, ignoreCase: true, out var kind)
            ? Create(kind)
            : new SqliteProvider();
    }
}
