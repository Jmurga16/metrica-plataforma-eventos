using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application;

namespace NotificationService.Infrastructure.Messaging;

public sealed class EventCreatedConsumer(
    EventNotificationProcessor processor,
    ILogger<EventCreatedConsumer> logger) : IConsumer<EventCreated>
{
    public async Task Consume(ConsumeContext<EventCreated> context)
    {
        var message = context.Message;
        var notification = new EventCreatedNotification(
            message.MessageId,
            message.EventId,
            message.Name,
            message.OccurredAt,
            message.CorrelationId,
            message.Version,
            message.Date,
            message.Venue,
            message.Status,
            message.Zones
                .Select(zone => new EventCreatedZoneNotification(zone.Name, zone.Price, zone.Capacity))
                .ToArray());

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["correlationId"] = message.CorrelationId,
                   ["messageId"] = message.MessageId
               }))
        {
            await processor.ProcessAsync(notification, context.CancellationToken);
        }
    }
}

public sealed class EventCreatedConsumerDefinition : ConsumerDefinition<EventCreatedConsumer>
{
    private readonly TimeSpan[] _retryIntervals;

    public EventCreatedConsumerDefinition(IOptions<RabbitMqOptions> options)
    {
        EndpointName = "notifications-event-created";
        _retryIntervals = options.Value.RetryIntervalsMilliseconds
            .Select(interval => TimeSpan.FromMilliseconds(interval))
            .ToArray();
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<EventCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Intervals(_retryIntervals);
            retry.Ignore<ArgumentException>();
        });
    }
}
