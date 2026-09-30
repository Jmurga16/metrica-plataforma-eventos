using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Application;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("NotificationsDb")
                ?? throw new InvalidOperationException("ConnectionStrings:NotificationsDb is required.");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationUnitOfWork>(provider =>
            provider.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<EventNotificationProcessor>();
        services.AddScoped<IEventEmailSender, MailKitEventEmailSender>();
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Bind(configuration.GetSection("Smtp"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.To), "At least one notification recipient is required.")
            .Validate(options => options.TimeoutSeconds > 0, "SMTP timeout must be positive.")
            .ValidateOnStart();
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => options.RetryIntervalsMilliseconds is { Length: > 0 } &&
                                 options.RetryIntervalsMilliseconds.All(interval => interval > 0),
                "At least one positive retry interval is required.")
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumer<EventCreatedConsumer, EventCreatedConsumerDefinition>();
            bus.AddConsumer<FaultEventCreatedConsumer, FaultEventCreatedConsumerDefinition>();
            bus.UsingRabbitMq((context, rabbit) =>
            {
                var rabbitMq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                rabbit.Host(rabbitMq.Host, rabbitMq.Port, rabbitMq.VirtualHost, host =>
                {
                    host.Username(rabbitMq.User);
                    host.Password(rabbitMq.Password);
                });
                rabbit.ConfigureEndpoints(context);
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<NotificationsDbContext>("postgresql", tags: ["ready"])
            .AddCheck<SmtpHealthCheck>("smtp", tags: ["ready"]);

        return services;
    }
}
