using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Persistence;
using Shouldly;

namespace NotificationService.IntegrationTests;

public sealed class NotificationApiTests(NotificationApiFixture fixture) : IClassFixture<NotificationApiFixture>
{
    [InfrastructureFact]
    public async Task Notifications_require_admin_role_and_support_event_filter_and_limit()
    {
        var selectedEventId = Guid.NewGuid();
        await SeedNotificationsAsync(selectedEventId);

        using var anonymous = fixture.Client;
        (await anonymous.GetAsync("/notifications")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var user = CreateAuthenticatedClient("User");
        (await user.GetAsync("/notifications")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var admin = CreateAuthenticatedClient("Admin");
        var response = await admin.GetAsync($"/notifications?eventId={selectedEventId}&limit=1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        var notification = notifications.ShouldHaveSingleItem();
        notification.EventId.ShouldBe(selectedEventId);
        notification.EventName.ShouldBe("Evento seleccionado");
    }

    [InfrastructureFact]
    public async Task Liveness_is_anonymous_and_returns_a_correlation_id()
    {
        using var client = fixture.Client;
        var response = await client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Guid.TryParse(response.Headers.GetValues("X-Correlation-Id").Single(), out _).ShouldBeTrue();
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = fixture.Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    private async Task SeedNotificationsAsync(Guid selectedEventId)
    {
        var now = DateTimeOffset.UtcNow;
        var selected = Notification.Create(
            Guid.NewGuid(), selectedEventId, "Evento seleccionado", now, Guid.NewGuid(),
            new string('a', 64), "{}", now);
        var other = Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Otro evento", now, Guid.NewGuid(),
            new string('b', 64), "{}", now.AddMinutes(-1));

        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        dbContext.Notifications.AddRange(selected, other);
        await dbContext.SaveChangesAsync();
    }

    private static string CreateToken(string role)
    {
        const string signingKey = "local-development-key-change-me-32-bytes-minimum";
        var token = new JwtSecurityToken(
            "events-platform-local",
            "events-api",
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
