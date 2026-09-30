using EventService.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace EventService.Api.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DomainException domain =>
                (StatusCodes.Status422UnprocessableEntity, "Regla de negocio no satisfecha", domain.Message),
            BadHttpRequestException =>
                (StatusCodes.Status400BadRequest, "Solicitud inválida", "El cuerpo de la solicitud no tiene el formato esperado."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado", "Use el traceId para solicitar soporte.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled error. TraceId: {TraceId}", context.TraceIdentifier);

        await Results.Problem(
                statusCode: status,
                title: title,
                detail: detail,
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
            .ExecuteAsync(context);
        return true;
    }
}
