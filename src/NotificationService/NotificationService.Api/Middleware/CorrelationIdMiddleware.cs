using Serilog.Context;

namespace NotificationService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = Guid.TryParse(header, out var parsed) ? parsed : Guid.NewGuid();
        context.Response.Headers[HeaderName] = correlationId.ToString();

        using (LogContext.PushProperty("correlationId", correlationId))
        {
            await next(context);
        }
    }
}
