using Contracts.Events;
using MassTransit;
using NotificationService.Application;

namespace NotificationService.Infrastructure.Messaging;

public sealed class EventCreatedConsumer(EventNotificationProcessor processor) : IConsumer<EventCreated>
{
    public Task Consume(ConsumeContext<EventCreated> context)
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

        return processor.ProcessAsync(notification, context.CancellationToken);
    }
}

public sealed class EventCreatedConsumerDefinition : ConsumerDefinition<EventCreatedConsumer>
{
    public EventCreatedConsumerDefinition() => EndpointName = "notifications-event-created";

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<EventCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15));
            retry.Ignore<ArgumentException>();
        });
    }
}
