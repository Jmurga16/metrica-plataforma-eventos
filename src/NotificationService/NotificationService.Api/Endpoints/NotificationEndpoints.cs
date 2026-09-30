using NotificationService.Application;

namespace NotificationService.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/notifications", async (
                Guid? eventId,
                int? limit,
                INotificationRepository repository,
                CancellationToken cancellationToken) =>
            {
                var take = Math.Clamp(limit ?? 100, 1, 200);
                var notifications = await repository.ListAsync(eventId, take, cancellationToken);
                return Results.Ok(notifications.Select(NotificationDto.From));
            })
            .RequireAuthorization(AuthorizationPolicies.InspectNotifications)
            .WithName("GetNotifications");
        return endpoints;
    }
}
