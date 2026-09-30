using Microsoft.EntityFrameworkCore;
using NotificationService.Application;
using NotificationService.Domain;

namespace NotificationService.Infrastructure.Persistence;

internal sealed class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public Task<Notification?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken) =>
        dbContext.Notifications.SingleOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken) =>
        await dbContext.Notifications.AddAsync(notification, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListAsync(
        Guid? eventId,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.Notifications
            .AsNoTracking()
            .Where(x => eventId == null || x.EventId == eventId)
            .OrderByDescending(x => x.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
