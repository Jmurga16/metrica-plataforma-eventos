using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace EventService.IntegrationTests;

public sealed class EventApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private RabbitMqContainer? _rabbitMq;
    private WebApplicationFactory<Program>? _factory;

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_INFRASTRUCTURE_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    public HttpClient Client => (_factory ?? throw new InvalidOperationException("Fixture was not initialized."))
        .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public IServiceProvider Services =>
        (_factory ?? throw new InvalidOperationException("Fixture was not initialized.")).Services;

    public async Task InitializeAsync()
    {
        if (!IsEnabled) return;

        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("events_db")
            .WithUsername("events_app")
            .WithPassword("events-integration-password")
            .Build();
        _redis = new RedisBuilder("redis:7-alpine").Build();
        _rabbitMq = new RabbitMqBuilder("rabbitmq:4-alpine")
            .WithUsername("events")
            .WithPassword("rabbitmq-integration-password")
            .Build();

        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _rabbitMq.StartAsync());

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:EventsDb"] = _postgres.GetConnectionString(),
            ["Redis:ConnectionString"] = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)},abortConnect=false",
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:User"] = "events",
            ["RabbitMq:Password"] = "rabbitmq-integration-password",
            ["Database:ApplyMigrationsOnStartup"] = "true"
        };

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
        });

        _ = _factory.Services;
    }

    public async Task DisposeAsync()
    {
        if (!IsEnabled) return;

        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(
            _rabbitMq!.DisposeAsync().AsTask(),
            _redis!.DisposeAsync().AsTask(),
            _postgres!.DisposeAsync().AsTask());
    }
}
