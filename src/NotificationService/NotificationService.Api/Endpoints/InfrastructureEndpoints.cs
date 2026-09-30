using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace NotificationService.Api.Endpoints;

public static class InfrastructureEndpoints
{
    public static IEndpointRouteBuilder MapNotificationApiInfrastructureEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();
        return app;
    }
}
