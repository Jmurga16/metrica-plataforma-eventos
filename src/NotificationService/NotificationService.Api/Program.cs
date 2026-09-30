using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Application;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) => configuration
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "notification-service")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "events-platform-local",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "events-api",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanInspectNotifications", policy => policy.RequireRole("Admin"));

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var header = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    var correlationId = Guid.TryParse(header, out var parsed) ? parsed : Guid.NewGuid();
    context.Response.Headers["X-Correlation-Id"] = correlationId.ToString();

    using (LogContext.PushProperty("correlationId", correlationId))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/notifications", async (
    Guid? eventId,
    int? limit,
    INotificationRepository repository,
    CancellationToken cancellationToken) =>
{
    var take = Math.Clamp(limit ?? 100, 1, 200);
    var notifications = await repository.ListAsync(eventId, take, cancellationToken);
    return Results.Ok(notifications.Select(NotificationDto.From));
})
    .RequireAuthorization("CanInspectNotifications")
    .WithName("GetNotifications");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready").AllowAnonymous();

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
