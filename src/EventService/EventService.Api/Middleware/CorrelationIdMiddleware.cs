using Serilog.Context;

namespace EventService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = Guid.TryParse(header, out var parsed) ? parsed : Guid.NewGuid();
        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId.ToString();

        using (LogContext.PushProperty("correlationId", correlationId))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdHttpContextExtensions
{
    public static Guid GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value) && value is Guid correlationId
            ? correlationId
            : throw new InvalidOperationException("Correlation ID middleware has not run.");
}
