using System.Collections.Concurrent;
using NotificationService.Application;

namespace NotificationService.IntegrationTests;

public sealed class ControlledEmailSender : IEventEmailSender
{
    private readonly ConcurrentDictionary<Guid, int> _failuresRemaining = new();
    private readonly ConcurrentDictionary<Guid, int> _calls = new();

    public void FailNext(Guid messageId, int attempts) =>
        _failuresRemaining[messageId] = attempts;

    public int CallsFor(Guid messageId) =>
        _calls.GetValueOrDefault(messageId);

    public Task SendAsync(EventCreatedNotification notification, CancellationToken cancellationToken)
    {
        _calls.AddOrUpdate(notification.MessageId, 1, (_, calls) => calls + 1);
        var shouldFail = _failuresRemaining.TryGetValue(notification.MessageId, out var remaining) && remaining > 0;
        if (shouldFail) _failuresRemaining[notification.MessageId] = remaining - 1;

        return shouldFail
            ? Task.FromException(new IOException("SMTP unavailable for integration test"))
            : Task.CompletedTask;
    }
}
