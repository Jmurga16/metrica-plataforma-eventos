using EventService.Application;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Domain.Event> Events => Set<Domain.Event>();
    public DbSet<Domain.Zone> Zones => Set<Domain.Zone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var events = modelBuilder.Entity<Domain.Event>();
        events.ToTable("events");
        events.HasKey(x => x.Id);
        events.Property(x => x.Name).HasMaxLength(150).IsRequired();
        events.Property(x => x.Venue).HasMaxLength(200).IsRequired();
        events.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        events.HasIndex(x => x.Date);
        events.HasMany(x => x.Zones).WithOne().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        events.Navigation(x => x.Zones).UsePropertyAccessMode(PropertyAccessMode.Field);

        var zones = modelBuilder.Entity<Domain.Zone>();
        zones.ToTable("zones");
        zones.HasKey(x => x.Id);
        zones.Property(x => x.Name).HasMaxLength(100).IsRequired();
        zones.Property(x => x.Price).HasPrecision(18, 2);
        zones.HasIndex(x => new { x.EventId, x.Name }).IsUnique();

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
