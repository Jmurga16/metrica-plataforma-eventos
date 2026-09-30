using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Contracts.Events;
using EventService.Application;
using EventService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace EventService.IntegrationTests;

public sealed class EventApiTests(EventApiFixture fixture) : IClassFixture<EventApiFixture>
{
    [InfrastructureFact]
    public async Task Post_requires_admin_and_persists_event_with_zones_transactionally()
    {
        using var anonymous = fixture.Client;
        var command = CreateCommand("Festival de integración");
        (await anonymous.PostAsJsonAsync("/events", command)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var user = await CreateAuthenticatedClientAsync("User");
        var userResponse = await user.PostAsJsonAsync("/events", command);
        userResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var publishedMessage = new TaskCompletionSource<EventCreated>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bus = fixture.Services.GetRequiredService<IBus>();
        var endpoint = bus.ConnectReceiveEndpoint(
            $"event-api-integration-{Guid.NewGuid():N}",
            configurator => configurator.Handler<EventCreated>(context =>
            {
                publishedMessage.TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        await endpoint.Ready;

        using var admin = await CreateAuthenticatedClientAsync("Admin");
        var response = await admin.PostAsJsonAsync("/events", command);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<EventCreatedResponse>();
        created.ShouldNotBeNull();
        var integrationEvent = await publishedMessage.Task.WaitAsync(TimeSpan.FromSeconds(10));
        integrationEvent.EventId.ShouldBe(created.Id);
        integrationEvent.CorrelationId.ShouldBe(Guid.Parse(response.Headers.GetValues("X-Correlation-Id").Single()));
        await endpoint.StopAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var entity = await scope.ServiceProvider.GetRequiredService<EventsDbContext>()
            .Events.Include(item => item.Zones)
            .SingleAsync(item => item.Id == created.Id);
        entity.Name.ShouldBe("Festival de integración");
        entity.Zones.Count.ShouldBe(2);
    }

    [InfrastructureFact]
    public async Task Get_uses_redis_cache_and_post_invalidates_the_cached_list()
    {
        using var admin = await CreateAuthenticatedClientAsync("Admin");
        (await admin.PostAsJsonAsync("/events", CreateCommand($"Evento {Guid.NewGuid():N}")))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        using var reader = await CreateAuthenticatedClientAsync("User");
        var first = await reader.GetAsync("/events?page=1&pageSize=20");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.GetValues("X-Cache").Single().ShouldBe("MISS");

        var second = await reader.GetAsync("/events?page=1&pageSize=20");
        second.Headers.GetValues("X-Cache").Single().ShouldBe("HIT");

        (await admin.PostAsJsonAsync("/events", CreateCommand($"Evento {Guid.NewGuid():N}")))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        var afterWrite = await reader.GetAsync("/events?page=1&pageSize=20");
        afterWrite.Headers.GetValues("X-Cache").Single().ShouldBe("MISS");
    }

    [InfrastructureFact]
    public async Task Post_returns_validation_problem_and_rate_limits_the_eleventh_request()
    {
        using var admin = await CreateAuthenticatedClientAsync("Admin");
        var invalid = new CreateEventCommand("", DateTimeOffset.UtcNow.AddDays(1), "", []);

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            var response = await admin.PostAsJsonAsync("/events", invalid);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        }

        (await admin.PostAsJsonAsync("/events", invalid)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        var client = fixture.Client;
        var tokenResponse = await client.PostAsJsonAsync("/auth/dev-token", new { role });
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>();
        token.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    private static CreateEventCommand CreateCommand(string name) => new(
        name,
        DateTimeOffset.UtcNow.AddMonths(2),
        "Arena Lima",
        [new CreateZoneRequest("VIP", 150m, 100), new CreateZoneRequest("General", 50m, 500)]);

    private sealed record DevTokenResponse(string AccessToken, int ExpiresIn);
}
