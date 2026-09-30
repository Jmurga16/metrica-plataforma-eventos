using EventService.Api;
using EventService.Api.Endpoints;
using EventService.Api.Middleware;
using EventService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) => configuration
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddEventApi(builder.Configuration);
builder.Services.AddEventInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapEventEndpoints();
app.MapDevelopmentAuthEndpoints();
app.MapEventApiInfrastructureEndpoints();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
