using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;
using NotificationService.Infrastructure.Persistence;
using Shouldly;

namespace NotificationService.IntegrationTests;

public sealed class NotificationsDbContextModelTests
{
    [Fact]
    public void Model_has_idempotency_and_json_payload_constraints()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test")
            .Options;
        using var context = new NotificationsDbContext(options);

        var entity = context.Model.FindEntityType(typeof(Notification));

        entity.ShouldNotBeNull();
        entity.GetIndexes().Single(index => index.IsUnique)
            .Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(Notification.MessageId));
        entity.FindProperty(nameof(Notification.Payload))!
            .GetColumnType().ShouldBe("jsonb");
    }
}
