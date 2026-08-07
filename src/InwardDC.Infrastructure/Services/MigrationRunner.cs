using InwardDC.Domain.Enums;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// Applies schema migrations on startup. Because every provider has its own
/// migrations assembly, switching database backends is a configuration change.
/// </summary>
public class MigrationRunner
{
    private readonly AppDbContext _db;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(AppDbContext db, ILogger<MigrationRunner> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.Database.MigrateAsync(ct);
            _logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations.");
            throw;
        }
    }
}
