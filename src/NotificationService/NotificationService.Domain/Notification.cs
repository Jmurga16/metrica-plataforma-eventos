namespace NotificationService.Domain;

public sealed class Notification
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid messageId,
        Guid eventId,
        string eventName,
        DateTimeOffset occurredAt,
        Guid correlationId,
        string payloadHash,
        string payload,
        DateTimeOffset receivedAt)
    {
        Id = id;
        MessageId = messageId;
        MessageType = "EventCreated";
        EventId = eventId;
        EventName = eventName;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        PayloadHash = payloadHash;
        Payload = payload;
        Channel = "Email";
        Status = NotificationStatus.Pending;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string MessageType { get; private set; } = null!;
    public Guid EventId { get; private set; }
    public string EventName { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string PayloadHash { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public string Channel { get; private set; } = null!;
    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public static Notification Create(
        Guid messageId,
        Guid eventId,
        string eventName,
        DateTimeOffset occurredAt,
        Guid correlationId,
        string payloadHash,
        string payload,
        DateTimeOffset receivedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        if (messageId == Guid.Empty) throw new ArgumentException("MessageId is required.", nameof(messageId));
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        if (correlationId == Guid.Empty) throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        if (payloadHash.Length != 64) throw new ArgumentException("PayloadHash must be a SHA-256 hexadecimal value.", nameof(payloadHash));

        return new Notification(
            Guid.CreateVersion7(), messageId, eventId, eventName.Trim(), occurredAt,
            correlationId, payloadHash, payload, receivedAt);
    }

    public void RegisterAttempt(string? error = null)
    {
        Attempts++;
        LastError = Truncate(error, 1000);
        Status = NotificationStatus.Pending;
    }

    public void RegisterAttemptFailure(string error)
    {
        LastError = Truncate(error, 1000);
        Status = NotificationStatus.Pending;
    }

    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        LastError = null;
    }

    public void MarkFailed(string? error = null)
    {
        if (Status == NotificationStatus.Sent)
        {
            return;
        }

        Status = NotificationStatus.Failed;
        LastError = Truncate(error, 1000) ?? LastError;
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sanitized = value.ReplaceLineEndings(" ").Trim();
        return sanitized.Length <= maximumLength ? sanitized : sanitized[..maximumLength];
    }
}
