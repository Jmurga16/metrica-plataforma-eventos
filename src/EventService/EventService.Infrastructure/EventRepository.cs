using EventService.Application;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure;

public sealed class EventRepository(EventsDbContext dbContext) : IEventRepository
{
    public void Add(Domain.Event entity) => dbContext.Events.Add(entity);

    public Task<Domain.Event?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Events.AsNoTracking().Include(x => x.Zones)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Event>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.Events.AsNoTracking().Include(x => x.Zones)
            .OrderBy(x => x.Date).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
}
