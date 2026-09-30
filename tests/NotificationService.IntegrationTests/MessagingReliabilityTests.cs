using System.Net.Http.Headers;
using System.Text.Json;
using Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Domain;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Shouldly;

namespace NotificationService.IntegrationTests;

public sealed class MessagingReliabilityTests(NotificationApiFixture fixture) : IClassFixture<NotificationApiFixture>
{
    [InfrastructureFact]
    public async Task Duplicate_message_is_persisted_and_sent_only_once()
    {
        var message = CreateMessage();
        var publisher = fixture.Services.GetRequiredService<IBus>();

        await publisher.Publish(message);
        await fixture.WaitForNotificationAsync(message.MessageId, item => item.Status == NotificationStatus.Sent);
        await publisher.Publish(message);
        await Task.Delay(500);

        await using var scope = fixture.Services.CreateAsyncScope();
        var matching = await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>()
            .Notifications.AsNoTracking()
            .Where(item => item.MessageId == message.MessageId)
            .ToListAsync();
        matching.ShouldHaveSingleItem();
        fixture.EmailSender.CallsFor(message.MessageId).ShouldBe(1);
    }

    [InfrastructureFact]
    public async Task Transient_smtp_failure_is_retried_and_eventually_sent()
    {
        fixture.Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value
            .RetryIntervalsMilliseconds.ShouldBe([10, 20, 30]);
        var message = CreateMessage();
        fixture.EmailSender.FailNext(message.MessageId, 1);

        await fixture.Services.GetRequiredService<IBus>().Publish(message);
        var notification = await fixture.WaitForNotificationAsync(
            message.MessageId,
            item => item.Status == NotificationStatus.Sent);

        notification.Attempts.ShouldBe(2);
        fixture.EmailSender.CallsFor(message.MessageId).ShouldBe(2);
    }

    [InfrastructureFact]
    public async Task Exhausted_retries_mark_failed_and_move_message_to_error_queue()
    {
        fixture.Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value
            .RetryIntervalsMilliseconds.ShouldBe([10, 20, 30]);
        var message = CreateMessage();
        fixture.EmailSender.FailNext(message.MessageId, 10);

        var faultReceived = new TaskCompletionSource<Fault<EventCreated>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bus = fixture.Services.GetRequiredService<IBus>();
        var endpoint = bus.ConnectReceiveEndpoint(
            $"notification-fault-test-{Guid.NewGuid():N}",
            configurator => configurator.Handler<Fault<EventCreated>>(context =>
            {
                faultReceived.TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        await endpoint.Ready;

        await bus.Publish(message);
        var fault = await faultReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        fault.Message.MessageId.ShouldBe(message.MessageId);
        var notification = await fixture.WaitForNotificationAsync(
            message.MessageId,
            item => item.Status == NotificationStatus.Failed);

        notification.Attempts.ShouldBe(4);
        fixture.EmailSender.CallsFor(message.MessageId).ShouldBe(4);
        (await WaitForErrorQueueMessageAsync()).ShouldBeGreaterThan(0);
        await endpoint.StopAsync();
    }

    private async Task<int> WaitForErrorQueueMessageAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(fixture.RabbitMqManagementUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String("events:rabbitmq-integration-password"u8.ToArray()));

        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync("/api/queues/%2F/notifications-event-created_error");
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.TryGetProperty("messages", out var messagesProperty))
                {
                    var messages = messagesProperty.GetInt32();
                    if (messages > 0) return messages;
                }
            }

            await Task.Delay(100);
        }

        return 0;
    }

    private static EventCreated CreateMessage() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Concierto de integración",
        DateTimeOffset.UtcNow,
        Guid.NewGuid(),
        1,
        DateTimeOffset.UtcNow.AddMonths(2),
        "Arena Lima",
        "Draft",
        [new EventCreatedZone("VIP", 150m, 100)]);
}
