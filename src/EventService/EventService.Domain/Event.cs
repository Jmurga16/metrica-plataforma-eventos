namespace EventService.Domain;

public sealed class Event
{
    private readonly List<Zone> _zones = [];
    private Event() { }

    public static Event Create(
        string name,
        DateTimeOffset date,
        string venue,
        IEnumerable<(string Name, decimal Price, int Capacity)> zones,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("event.name_required", "El nombre es obligatorio.");
        if (name.Trim().Length > 150) throw new DomainException("event.name_too_long", "El nombre admite hasta 150 caracteres.");
        if (date <= now) throw new DomainException("event.date_in_past", "La fecha debe estar en el futuro.");
        if (string.IsNullOrWhiteSpace(venue)) throw new DomainException("event.venue_required", "El lugar es obligatorio.");

        var zoneList = zones.ToList();
        if (zoneList.Count == 0) throw new DomainException("event.zones_required", "Debe registrar al menos una zona.");
        if (zoneList.Select(x => x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zoneList.Count)
            throw new DomainException("event.zone_duplicated", "Los nombres de las zonas no pueden repetirse.");

        var entity = new Event
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Date = date.ToUniversalTime(),
            Venue = venue.Trim(),
            Status = EventStatus.Draft,
            CreatedAt = now.ToUniversalTime()
        };

        entity._zones.AddRange(zoneList.Select(x => new Zone(entity.Id, x.Name, x.Price, x.Capacity)));
        return entity;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset Date { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public EventStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Zone> Zones => _zones;
}
