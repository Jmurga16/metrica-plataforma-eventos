using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application;
using NotificationService.Domain;

namespace NotificationService.Infrastructure.Messaging;

public sealed class FaultEventCreatedConsumer(
    INotificationRepository repository,
    INotificationUnitOfWork unitOfWork,
    ILogger<FaultEventCreatedConsumer> logger) : IConsumer<Fault<EventCreated>>
{
    public async Task Consume(ConsumeContext<Fault<EventCreated>> context)
    {
        var message = context.Message.Message;
        var notification = await repository.GetByMessageIdAsync(message.MessageId, context.CancellationToken);

        if (notification is null || notification.Status == NotificationStatus.Sent)
        {
            logger.LogWarning(
                "Fault received without a pending notification for MessageId {MessageId}",
                message.MessageId);
            return;
        }

        var lastError = context.Message.Exceptions.LastOrDefault()?.Message;
        notification.MarkFailed(lastError);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogError(
            "Notification marked Failed after retries for MessageId {MessageId} and CorrelationId {CorrelationId}",
            message.MessageId, message.CorrelationId);
    }
}

public sealed class FaultEventCreatedConsumerDefinition : ConsumerDefinition<FaultEventCreatedConsumer>
{
    public FaultEventCreatedConsumerDefinition() => EndpointName = "notifications-event-created-fault";
}
