using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationService.Domain;

namespace NotificationService.Application;

public sealed class EventNotificationProcessor(
    INotificationRepository repository,
    INotificationUnitOfWork unitOfWork,
    IEventEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<EventNotificationProcessor> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(EventCreatedNotification message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var payloadHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var notification = await repository.GetByMessageIdAsync(message.MessageId, cancellationToken);

        if (notification?.Status == NotificationStatus.Sent)
        {
            logger.LogInformation(
                "Duplicate EventCreated ignored for MessageId {MessageId} and CorrelationId {CorrelationId}",
                message.MessageId, message.CorrelationId);
            return;
        }

        if (notification is not null && !string.Equals(notification.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "EventCreated with MessageId {MessageId} has a different payload and was ignored",
                message.MessageId);
            return;
        }

        if (notification is null)
        {
            notification = Notification.Create(
                message.MessageId,
                message.EventId,
                message.Name,
                message.OccurredAt,
                message.CorrelationId,
                payloadHash,
                payload,
                timeProvider.GetUtcNow());

            await repository.AddAsync(notification, cancellationToken);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateMessageException)
            {
                unitOfWork.ClearTracking();
                notification = await repository.GetByMessageIdAsync(message.MessageId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Notification '{message.MessageId}' was not found after an idempotency conflict.");

                if (notification.Status == NotificationStatus.Sent ||
                    !string.Equals(notification.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    return;
                }
            }
        }

        notification.RegisterAttempt();

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
            notification.MarkSent(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Notification sent for MessageId {MessageId} and CorrelationId {CorrelationId}",
                message.MessageId, message.CorrelationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            notification.RegisterAttemptFailure(exception.Message);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(exception, "Notification attempt failed for MessageId {MessageId}", message.MessageId);
            throw;
        }
    }
}
