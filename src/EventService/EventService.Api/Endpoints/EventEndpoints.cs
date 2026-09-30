using EventService.Api.Middleware;
using EventService.Application;
using FluentValidation;

namespace EventService.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var events = endpoints.MapGroup("/events").WithTags("Events");
        events.MapPost("/", CreateEventAsync)
            .RequireAuthorization(AuthorizationPolicies.ManageEvents)
            .RequireRateLimiting(RateLimitPolicies.Writes);
        events.MapGet("/", ListEventsAsync)
            .RequireAuthorization(AuthorizationPolicies.ReadEvents);
        events.MapGet("/{id:guid}", GetEventAsync)
            .RequireAuthorization(AuthorizationPolicies.ReadEvents);
        return endpoints;
    }

    private static async Task<IResult> CreateEventAsync(
        CreateEventCommand command,
        IValidator<CreateEventCommand> validator,
        EventUseCases useCases,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        var result = await useCases.CreateAsync(command, context.GetCorrelationId(), cancellationToken);
        return Results.Created($"/events/{result.Id}", result);
    }

    private static async Task<IResult> ListEventsAsync(
        int? page,
        int? pageSize,
        EventUseCases useCases,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await useCases.ListAsync(
            Math.Max(1, page ?? 1),
            Math.Clamp(pageSize ?? 20, 1, 100),
            cancellationToken);
        context.Response.Headers["X-Cache"] = result.CacheHit ? "HIT" : "MISS";
        return Results.Ok(result.Events);
    }

    private static async Task<IResult> GetEventAsync(
        Guid id,
        EventUseCases useCases,
        CancellationToken cancellationToken) =>
        await useCases.GetAsync(id, cancellationToken) is { } result
            ? Results.Ok(result)
            : Results.NotFound();
}
