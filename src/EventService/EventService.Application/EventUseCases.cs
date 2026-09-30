using Contracts.Events;

namespace EventService.Application;

public sealed class EventUseCases(
    IEventRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IEventsCache cache,
    TimeProvider timeProvider)
{
    public async Task<EventCreatedResponse> CreateAsync(CreateEventCommand command, Guid correlationId, CancellationToken cancellationToken)
    {
        var entity = Domain.Event.Create(
            command.Name,
            command.Date,
            command.Venue,
            command.Zones.Select(x => (x.Name, x.Price, x.Capacity)),
            timeProvider.GetUtcNow());

        repository.Add(entity);
        var message = new EventCreated(
            Guid.NewGuid(), entity.Id, entity.Name, timeProvider.GetUtcNow(), correlationId, 1,
            entity.Date, entity.Venue, entity.Status.ToString(),
            entity.Zones.Select(z => new EventCreatedZone(z.Name, z.Price, z.Capacity)).ToList());
        await publisher.PublishAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateAsync(cancellationToken);
        return new EventCreatedResponse(entity.Id);
    }

    public async Task<(IReadOnlyList<EventDto> Events, bool CacheHit)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(page, pageSize, cancellationToken);
        if (cached.Value is not null) return (cached.Value, cached.Hit);

        var result = (await repository.ListAsync(page, pageSize, cancellationToken)).Select(x => x.ToDto()).ToList();
        await cache.SetAsync(page, pageSize, result, cancellationToken);
        return (result, false);
    }

    public async Task<EventDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        (await repository.GetAsync(id, cancellationToken))?.ToDto();
}
