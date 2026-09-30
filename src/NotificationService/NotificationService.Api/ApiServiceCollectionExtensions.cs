using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NotificationService.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddOpenApi();

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "events-platform-local",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "events-api",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.InspectNotifications, policy => policy.RequireRole("Admin"));
        return services;
    }
}

public static class AuthorizationPolicies
{
    public const string InspectNotifications = "CanInspectNotifications";
}
