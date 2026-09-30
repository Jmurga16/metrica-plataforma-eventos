namespace NotificationService.Application;

public sealed record EventCreatedNotification(
    Guid MessageId,
    Guid EventId,
    string Name,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    int Version,
    DateTimeOffset Date,
    string Venue,
    string Status,
    IReadOnlyList<EventCreatedZoneNotification> Zones);

public sealed record EventCreatedZoneNotification(string Name, decimal Price, int Capacity);
