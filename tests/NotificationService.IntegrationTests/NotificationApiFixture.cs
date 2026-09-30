using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace NotificationService.IntegrationTests;

public sealed class NotificationApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RabbitMqContainer? _rabbitMq;
    private WebApplicationFactory<Program>? _factory;

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_INFRASTRUCTURE_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    public IServiceProvider Services =>
        (_factory ?? throw new InvalidOperationException("Fixture was not initialized.")).Services;

    public ControlledEmailSender EmailSender => Services.GetRequiredService<ControlledEmailSender>();

    public string RabbitMqManagementUrl =>
        $"http://{_rabbitMq!.Hostname}:{_rabbitMq.GetMappedPublicPort(15672)}";

    public async ValueTask InitializeAsync()
    {
        if (!IsEnabled) return;

        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("notifications_db")
            .WithUsername("notifications_app")
            .WithPassword("notifications-integration-password")
            .Build();
        _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
            .WithUsername("events")
            .WithPassword("rabbitmq-integration-password")
            .WithPortBinding(15672, true)
            .Build();

        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotificationsDb"] = _postgres.GetConnectionString(),
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:User"] = "events",
            ["RabbitMq:Password"] = "rabbitmq-integration-password",
            ["RabbitMq:RetryIntervalsMilliseconds:0"] = "10",
            ["RabbitMq:RetryIntervalsMilliseconds:1"] = "20",
            ["RabbitMq:RetryIntervalsMilliseconds:2"] = "30",
            ["Database:ApplyMigrationsOnStartup"] = "true"
        };

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventEmailSender>();
                services.AddSingleton<ControlledEmailSender>();
                services.AddSingleton<IEventEmailSender>(provider => provider.GetRequiredService<ControlledEmailSender>());
            });
        });

        _ = _factory.Services;
    }

    public async Task<Notification> WaitForNotificationAsync(
        Guid messageId,
        Func<Notification, bool> predicate,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        Notification? lastSeen = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var scope = Services.CreateAsyncScope();
            var notification = await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>()
                .Notifications.AsNoTracking()
                .SingleOrDefaultAsync(item => item.MessageId == messageId);
            lastSeen = notification;
            if (notification is not null && predicate(notification)) return notification;
            await Task.Delay(50);
        }

        throw new TimeoutException(
            $"Notification {messageId} did not reach the expected state. " +
            $"Last state: {lastSeen?.Status.ToString() ?? "not found"}; attempts: {lastSeen?.Attempts}.");
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsEnabled) return;

        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(_rabbitMq!.DisposeAsync().AsTask(), _postgres!.DisposeAsync().AsTask());
    }
}
