using NotificationService.Domain;

namespace NotificationService.Application;

public sealed record NotificationDto(
    Guid Id,
    Guid MessageId,
    Guid EventId,
    string EventName,
    Guid CorrelationId,
    string Channel,
    string Status,
    int Attempts,
    string? LastError,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SentAt)
{
    public static NotificationDto From(Notification notification) => new(
        notification.Id,
        notification.MessageId,
        notification.EventId,
        notification.EventName,
        notification.CorrelationId,
        notification.Channel,
        notification.Status.ToString(),
        notification.Attempts,
        notification.LastError,
        notification.ReceivedAt,
        notification.SentAt);
}
