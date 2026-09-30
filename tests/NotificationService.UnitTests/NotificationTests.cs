using NotificationService.Domain;
using Shouldly;

namespace NotificationService.UnitTests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_initializes_pending_notification()
    {
        var notification = CreateNotification();

        notification.Status.ShouldBe(NotificationStatus.Pending);
        notification.Attempts.ShouldBe(0);
        notification.Channel.ShouldBe("Email");
        notification.MessageType.ShouldBe("EventCreated");
    }

    [Fact]
    public void MarkSent_clears_previous_error()
    {
        var notification = CreateNotification();
        notification.RegisterAttempt();
        notification.RegisterAttemptFailure("temporary failure");
        var sentAt = DateTimeOffset.Parse("2026-09-30T15:00:00Z");

        notification.MarkSent(sentAt);

        notification.Status.ShouldBe(NotificationStatus.Sent);
        notification.Attempts.ShouldBe(1);
        notification.LastError.ShouldBeNull();
        notification.SentAt.ShouldBe(sentAt);
    }

    [Fact]
    public void MarkFailed_does_not_downgrade_sent_notification()
    {
        var notification = CreateNotification();
        notification.MarkSent(DateTimeOffset.UtcNow);

        notification.MarkFailed("late fault");

        notification.Status.ShouldBe(NotificationStatus.Sent);
    }

    private static Notification CreateNotification() => Notification.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Concierto",
        DateTimeOffset.Parse("2026-09-30T14:00:00Z"),
        Guid.NewGuid(),
        new string('a', 64),
        "{}",
        DateTimeOffset.Parse("2026-09-30T14:00:01Z"));
}
