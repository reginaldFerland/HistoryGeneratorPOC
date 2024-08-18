using Generated.Data.Models;
using HistoryGeneratorPOC.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Generated.Data;
public partial class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        TrackHistory<User, UserHistory>(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    private void TrackHistory<TEntity, THistory>(DbContext context)
        where TEntity : class
        where THistory : BaseHistory, new()
    {
        var historyEntries = context.ChangeTracker.Entries()
            .Where(x => x.Entity is TEntity
            && (x.State is EntityState.Added
            || x.State is EntityState.Modified
            || x.State is EntityState.Deleted))
            .Select(x => new THistory
            {
                 Id = new Random().Next(),
                 Data = JsonSerializer.Serialize(x.Entity),
                 UpdatedAt = x.CurrentValues.GetValue<DateTime>("UpdatedAt")
            });
        if (historyEntries.Any())
        {
            context.Set<THistory>().AddRange(historyEntries.ToList());
        }
    }
}
