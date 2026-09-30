using Contracts.Events;
using EventService.Application;
using MassTransit;

namespace EventService.Infrastructure;

public sealed class IntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync(EventCreated message, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(message, context =>
        {
            context.MessageId = message.MessageId;
            context.CorrelationId = message.CorrelationId;
        }, cancellationToken);
}
