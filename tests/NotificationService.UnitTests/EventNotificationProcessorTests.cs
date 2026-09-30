using Microsoft.Extensions.Logging.Abstractions;
using NotificationService.Application;
using NotificationService.Domain;
using Shouldly;

namespace NotificationService.UnitTests;

public sealed class EventNotificationProcessorTests
{
    [Fact]
    public async Task New_message_is_persisted_and_sent_once()
    {
        var fixture = new ProcessorFixture();

        await fixture.Processor.ProcessAsync(fixture.Message, TestContext.Current.CancellationToken);

        fixture.Repository.Items.ShouldHaveSingleItem();
        fixture.Repository.Items[0].Status.ShouldBe(NotificationStatus.Sent);
        fixture.Repository.Items[0].Attempts.ShouldBe(1);
        fixture.EmailSender.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Sent_duplicate_is_acknowledged_without_another_email()
    {
        var fixture = new ProcessorFixture();
        await fixture.Processor.ProcessAsync(fixture.Message, TestContext.Current.CancellationToken);

        await fixture.Processor.ProcessAsync(fixture.Message, TestContext.Current.CancellationToken);

        fixture.Repository.Items.ShouldHaveSingleItem();
        fixture.EmailSender.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Smtp_failure_is_recorded_and_rethrown_for_transport_retry()
    {
        var fixture = new ProcessorFixture();
        fixture.EmailSender.Exception = new IOException("SMTP unavailable");

        var exception = await Should.ThrowAsync<IOException>(() =>
            fixture.Processor.ProcessAsync(fixture.Message, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("SMTP unavailable");
        var notification = fixture.Repository.Items.ShouldHaveSingleItem();
        notification.Status.ShouldBe(NotificationStatus.Pending);
        notification.Attempts.ShouldBe(1);
        notification.LastError.ShouldBe("SMTP unavailable");
    }

    private sealed class ProcessorFixture
    {
        public ProcessorFixture()
        {
            Processor = new EventNotificationProcessor(
                Repository,
                UnitOfWork,
                EmailSender,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-09-30T15:00:00Z")),
                NullLogger<EventNotificationProcessor>.Instance);
        }

        public InMemoryNotificationRepository Repository { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public FakeEmailSender EmailSender { get; } = new();
        public EventNotificationProcessor Processor { get; }
        public EventCreatedNotification Message { get; } = new(
            Guid.Parse("01990000-0000-7000-8000-000000000001"),
            Guid.Parse("01990000-0000-7000-8000-000000000002"),
            "Concierto",
            DateTimeOffset.Parse("2026-09-30T14:00:00Z"),
            Guid.Parse("01990000-0000-7000-8000-000000000003"),
            1,
            DateTimeOffset.Parse("2026-12-20T20:00:00Z"),
            "Arena Lima",
            "Draft",
            [new EventCreatedZoneNotification("VIP", 150m, 100)]);
    }

    private sealed class InMemoryNotificationRepository : INotificationRepository
    {
        public List<Notification> Items { get; } = [];

        public Task<Notification?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(x => x.MessageId == messageId));

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Items.Add(notification);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> ListAsync(
            Guid? eventId,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Notification>>(Items.Take(limit).ToArray());
    }

    private sealed class FakeUnitOfWork : INotificationUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public void ClearTracking()
        {
        }
    }

    private sealed class FakeEmailSender : IEventEmailSender
    {
        public int CallCount { get; private set; }
        public Exception? Exception { get; set; }

        public Task SendAsync(EventCreatedNotification notification, CancellationToken cancellationToken)
        {
            CallCount++;
            return Exception is null ? Task.CompletedTask : Task.FromException(Exception);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
