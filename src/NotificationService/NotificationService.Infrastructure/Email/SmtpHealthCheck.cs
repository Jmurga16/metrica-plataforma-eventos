using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Email;

internal sealed class SmtpHealthCheck(IOptions<EmailOptions> options) : IHealthCheck
{
    private readonly EmailOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Min(_options.TimeoutSeconds, 5)));
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None,
                timeoutSource.Token);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(
                    _options.Username,
                    _options.Password ?? string.Empty,
                    timeoutSource.Token);
            }

            await client.DisconnectAsync(true, timeoutSource.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SMTP is unavailable.", exception);
        }
    }
}
