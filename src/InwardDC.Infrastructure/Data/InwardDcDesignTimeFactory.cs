using InwardDC.Domain.Enums;
using InwardDC.Infrastructure.Common;
using InwardDC.Infrastructure.Data;
using InwardDC.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InwardDC.Infrastructure.Data;

/// <summary>
/// Design-time factory used by `dotnet ef migrations add` / `dotnet ef database
/// update`. Reads the same appsettings.json the app uses so migrations target the
/// correct provider. Override with environment variables or a --provider argument.
/// </summary>
public class InwardDcDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var kind = configuration["Database:Provider"] is { Length: > 0 } p
            ? Enum.Parse<DatabaseProviderKind>(p, ignoreCase: true)
            : DatabaseProviderKind.SQLite;

        var provider = DatabaseProviderFactory.Create(kind);

        var dataDir = configuration["App:DataDirectory"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InwardDC");

        var paths = new AppPaths(dataDir);
        var connectionString = provider.GetConnectionString(
            paths.DatabaseDirectory,
            configuration["Database:ConnectionString"]);

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        provider.Configure(builder, connectionString, typeof(AppDbContext).Assembly.GetName().Name!);

        return new AppDbContext(builder.Options);
    }
}
