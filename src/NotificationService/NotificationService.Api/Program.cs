using Microsoft.EntityFrameworkCore;
using NotificationService.Api;
using NotificationService.Api.Endpoints;
using NotificationService.Api.Middleware;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) => configuration
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "notification-service")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddNotificationApi(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapNotificationEndpoints();
app.MapNotificationApiInfrastructureEndpoints();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
