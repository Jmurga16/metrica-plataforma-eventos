namespace EventService.Application;

public sealed record CreateEventCommand(string Name, DateTimeOffset Date, string Venue, IReadOnlyList<CreateZoneRequest> Zones);
public sealed record CreateZoneRequest(string Name, decimal Price, int Capacity);
public sealed record EventCreatedResponse(Guid Id);
public sealed record EventDto(Guid Id, string Name, DateTimeOffset Date, string Venue, string Status, IReadOnlyList<ZoneDto> Zones);
public sealed record ZoneDto(Guid Id, string Name, decimal Price, int Capacity);

public static class EventMappings
{
    public static EventDto ToDto(this Domain.Event entity) => new(
        entity.Id, entity.Name, entity.Date, entity.Venue, entity.Status.ToString(),
        entity.Zones.Select(z => new ZoneDto(z.Id, z.Name, z.Price, z.Capacity)).ToList());
}
