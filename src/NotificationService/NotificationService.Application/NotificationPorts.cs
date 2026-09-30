using NotificationService.Domain;

namespace NotificationService.Application;

public interface INotificationRepository
{
    Task<Notification?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> ListAsync(Guid? eventId, int limit, CancellationToken cancellationToken);
}

public interface INotificationUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void ClearTracking();
}

public interface IEventEmailSender
{
    Task SendAsync(EventCreatedNotification notification, CancellationToken cancellationToken);
}

public sealed class DuplicateMessageException(Guid messageId, Exception innerException)
    : Exception($"A notification for message '{messageId}' already exists.", innerException)
{
    public Guid MessageId { get; } = messageId;
}
