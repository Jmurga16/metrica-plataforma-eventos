using EventService.Domain;
using Shouldly;

namespace EventService.Domain.Tests;

public sealed class EventTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_requires_at_least_one_zone()
    {
        var action = () => Event.Create("Evento", Now.AddDays(1), "Lima", [], Now);
        action.ShouldThrow<DomainException>().Code.ShouldBe("event.zones_required");
    }

    [Fact]
    public void Create_rejects_duplicate_zone_names_ignoring_case()
    {
        var zones = new[] { ("VIP", 100m, 10), ("vip", 80m, 20) };
        var action = () => Event.Create("Evento", Now.AddDays(1), "Lima", zones, Now);
        action.ShouldThrow<DomainException>().Code.ShouldBe("event.zone_duplicated");
    }

    [Fact]
    public void Create_rejects_past_date()
    {
        var action = () => Event.Create("Evento", Now.AddMinutes(-1), "Lima", [("VIP", 100m, 10)], Now);
        action.ShouldThrow<DomainException>().Code.ShouldBe("event.date_in_past");
    }

    [Fact]
    public void Create_builds_draft_event_and_zones()
    {
        var entity = Event.Create(" Evento ", Now.AddDays(1), " Lima ", [("VIP", 100.125m, 10)], Now);
        entity.Name.ShouldBe("Evento");
        entity.Venue.ShouldBe("Lima");
        entity.Status.ShouldBe(EventStatus.Draft);
        entity.Zones.Single().Price.ShouldBe(100.12m);
    }
}
