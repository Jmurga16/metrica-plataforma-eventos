using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using EventService.Application;
using EventService.Domain;
using EventService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) => configuration
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<EventUseCases>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventValidator>();
builder.Services.AddEventInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<EventsDbContext>();

var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "local-development-signing-key-change-me-123456789";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "events-platform-local";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "events-api";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30),
        RoleClaimType = ClaimTypes.Role
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageEvents", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanReadEvents", policy => policy.RequireRole("Admin", "User"));
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins((builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000").Split(',', StringSplitOptions.RemoveEmptyEntries))
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("writes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (!Guid.TryParse(correlationId, out var parsed)) parsed = Guid.NewGuid();
    context.Items["CorrelationId"] = parsed;
    context.Response.Headers["X-Correlation-Id"] = parsed.ToString();
    using (LogContext.PushProperty("correlationId", parsed))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapPost("/auth/dev-token", (DevTokenRequest request) =>
    {
        if (request.Role is not ("Admin" or "User")) return Results.BadRequest(new { error = "El rol debe ser Admin o User." });
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, $"Demo {request.Role}"),
            new Claim(ClaimTypes.Role, request.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(jwtIssuer, jwtAudience, claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256));
        return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresIn = 3600 });
    }).AllowAnonymous();
}

var events = app.MapGroup("/events").WithTags("Events");
events.MapPost("/", async (CreateEventCommand command, IValidator<CreateEventCommand> validator, EventUseCases useCases, HttpContext context, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(command, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.Errors.GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));

    var result = await useCases.CreateAsync(command, (Guid)context.Items["CorrelationId"]!, ct);
    return Results.Created($"/events/{result.Id}", result);
}).RequireAuthorization("CanManageEvents").RequireRateLimiting("writes");

events.MapGet("/", async (int? page, int? pageSize, EventUseCases useCases, HttpContext context, CancellationToken ct) =>
{
    var result = await useCases.ListAsync(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 20, 1, 100), ct);
    context.Response.Headers["X-Cache"] = result.CacheHit ? "HIT" : "MISS";
    return Results.Ok(result.Events);
}).RequireAuthorization("CanReadEvents");

events.MapGet("/{id:guid}", async (Guid id, EventUseCases useCases, CancellationToken ct) =>
    await useCases.GetAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
    .RequireAuthorization("CanReadEvents");

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public sealed record DevTokenRequest(string Role);

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DomainException domain => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio no satisfecha", domain.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Solicitud inválida", "El cuerpo de la solicitud no tiene el formato esperado."),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado", "Use el traceId para solicitar soporte.")
        };
        if (status == 500) logger.LogError(exception, "Unhandled error. TraceId: {TraceId}", context.TraceIdentifier);
        await Results.Problem(statusCode: status, title: title, detail: detail,
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return true;
    }
}
