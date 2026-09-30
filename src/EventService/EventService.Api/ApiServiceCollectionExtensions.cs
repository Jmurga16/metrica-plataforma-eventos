using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using EventService.Api.Errors;
using EventService.Application;
using EventService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddEventApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<EventUseCases>();
        services.AddValidatorsFromAssemblyContaining<CreateEventValidator>();
        services.AddHealthChecks().AddDbContextCheck<EventsDbContext>();

        AddAuthentication(services, configuration);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.ManageEvents, policy => policy.RequireRole("Admin"));
            options.AddPolicy(AuthorizationPolicies.ReadEvents, policy => policy.RequireRole("Admin", "User"));
        });
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins((configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .AllowAnyHeader()
            .AllowAnyMethod()));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Writes, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "events-platform-local",
                ValidAudience = configuration["Jwt:Audience"] ?? "events-api",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role
            };
        });
    }
}

public static class AuthorizationPolicies
{
    public const string ManageEvents = "CanManageEvents";
    public const string ReadEvents = "CanReadEvents";
}

public static class RateLimitPolicies
{
    public const string Writes = "writes";
}
