using InwardDC.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Providers;

/// <summary>
/// Database provider abstraction. Every supported backend implements this contract,
/// so the rest of the application is completely provider agnostic. Swapping the
/// underlying database is a configuration change, not a code change.
/// </summary>
public interface IDatabaseProvider
{
    DatabaseProviderKind Kind { get; }
    string DisplayName { get; }

    /// <summary>Default connection string template for the provider.</summary>
    string DefaultConnectionString { get; }

    /// <summary>
    /// Resolves the effective connection string. If the user did not supply an
    /// explicit one, a provider specific default pointing at the data directory is used.
    /// </summary>
    string GetConnectionString(string dataDirectory, string? overrideConnectionString);

    /// <summary>Configures the DbContextOptionsBuilder for this provider.</summary>
    void Configure(DbContextOptionsBuilder options, string connectionString, string migrationsAssembly);

    bool CanCreateDatabase { get; }
}
