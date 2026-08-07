using InwardDC.Application.Interfaces;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Common;
using InwardDC.Infrastructure.Data;
using InwardDC.Infrastructure.Providers;
using InwardDC.Infrastructure.Repositories;
using InwardDC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.Infrastructure;

/// <summary>
/// Infrastructure composition root. Registers the data access stack (providers,
/// DbContext, repositories / unit of work) and the infrastructure services.
///
/// THE PORTABILITY SWITCH: change <c>Database:Provider</c> in appsettings.json
/// (SQLite / PostgreSQL / SqlServer / MySQL) and nothing else needs to change —
/// not the UI, not the business logic, not the repositories.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var appPaths = new AppPaths(configuration["App:DataDirectory"]);
        services.AddSingleton(appPaths);

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();
        services.AddSingleton(databaseOptions);

        var provider = DatabaseProviderFactory.Create(databaseOptions.Provider);
        services.AddSingleton(provider);

        var connectionString = provider.GetConnectionString(
            appPaths.DatabaseDirectory,
            string.IsNullOrWhiteSpace(databaseOptions.ConnectionString) ? null : databaseOptions.ConnectionString);

        services.AddDbContext<AppDbContext>(options =>
            provider.Configure(options, connectionString, typeof(AppDbContext).Assembly.GetName().Name!));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<MigrationRunner>();
        services.AddScoped<SeedService>();

        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }
}
