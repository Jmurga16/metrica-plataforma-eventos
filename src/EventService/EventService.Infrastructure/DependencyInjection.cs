using EventService.Application;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsDb")));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EventsDbContext>());
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        services.AddScoped<IEventsCache, EventsCache>();
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379");

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddEntityFrameworkOutbox<EventsDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", "/", host =>
                {
                    host.Username(configuration["RabbitMq:User"] ?? "guest");
                    host.Password(configuration["RabbitMq:Password"] ?? "guest");
                });
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
