using InwardDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

/// <summary>
/// Update helper for desktop-style (long lived) DbContext lifetimes. Repository
/// reads are AsNoTracking; calling DbSet.Update on an entity that is already
/// tracked under the same key throws. This merges values onto the tracked instance
/// instead, so get-then-update flows never collide.
/// </summary>
public static class UpdateHelper
{
    public static void UpdateTracked<T>(this DbContext db, T entity) where T : EntityBase
    {
        var tracked = db.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (tracked is null)
        {
            db.Update(entity);
            return;
        }

        if (!ReferenceEquals(tracked.Entity, entity))
        {
            tracked.CurrentValues.SetValues(entity);
            tracked.State = EntityState.Modified;
        }
        else
        {
            tracked.State = EntityState.Modified;
        }
    }
}
