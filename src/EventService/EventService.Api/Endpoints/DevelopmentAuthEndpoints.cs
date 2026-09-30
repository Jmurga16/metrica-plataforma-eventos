using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Api.Endpoints;

public static class DevelopmentAuthEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentAuthEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.MapPost("/auth/dev-token", (DevTokenRequest request, IConfiguration configuration) =>
        {
            if (request.Role is not ("Admin" or "User"))
                return Results.BadRequest(new { error = "El rol debe ser Admin o User." });

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, $"Demo {request.Role}"),
                new Claim(ClaimTypes.Role, request.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var key = configuration["Jwt:SigningKey"]
                ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
            var token = new JwtSecurityToken(
                configuration["Jwt:Issuer"] ?? "events-platform-local",
                configuration["Jwt:Audience"] ?? "events-api",
                claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256));
            return Results.Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                expiresIn = 3600
            });
        }).AllowAnonymous();

        return app;
    }
}

public sealed record DevTokenRequest(string Role);
