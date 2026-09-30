using MassTransit;

namespace Contracts.Events;

[EntityName("event-created.v1")]
[MessageUrn("event-created:v1")]
public sealed record EventCreated(
    Guid MessageId,
    Guid EventId,
    string Name,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    int Version,
    DateTimeOffset Date,
    string Venue,
    string Status,
    IReadOnlyList<EventCreatedZone> Zones);

public sealed record EventCreatedZone(string Name, decimal Price, int Capacity);
