namespace EventService.Domain;

public sealed class Zone
{
    private Zone() { }

    internal Zone(Guid eventId, string name, decimal price, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("zone.name_required", "El nombre de la zona es obligatorio.");
        if (price < 0) throw new DomainException("zone.invalid_price", "El precio no puede ser negativo.");
        if (capacity <= 0) throw new DomainException("zone.invalid_capacity", "La capacidad debe ser mayor que cero.");

        Id = Guid.NewGuid();
        EventId = eventId;
        Name = name.Trim();
        Price = decimal.Round(price, 2);
        Capacity = capacity;
    }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Capacity { get; private set; }
}
