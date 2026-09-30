using Contracts.Events;
using EventService.Domain;

namespace EventService.Application;

public interface IEventRepository
{
    void Add(Event entity);
    Task<Event?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Event>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IIntegrationEventPublisher
{
    Task PublishAsync(EventCreated message, CancellationToken cancellationToken);
}

public interface IEventsCache
{
    Task<(IReadOnlyList<EventDto>? Value, bool Hit)> GetAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task SetAsync(int page, int pageSize, IReadOnlyList<EventDto> events, CancellationToken cancellationToken);
    Task InvalidateAsync(CancellationToken cancellationToken);
}
