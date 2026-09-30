using System.Globalization;
using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Application;

namespace NotificationService.Infrastructure.Email;

internal sealed class MailKitEventEmailSender(IOptions<EmailOptions> options) : IEventEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EventCreatedNotification notification, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_options.From));
        foreach (var recipient in _options.To.Split(
                     [';', ','],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            email.To.Add(MailboxAddress.Parse(recipient));
        }

        email.Subject = $"Nuevo evento registrado: {notification.Name}";
        email.Body = BuildBody(notification);

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, timeoutSource.Token);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, timeoutSource.Token);
        }

        await client.SendAsync(email, timeoutSource.Token);
        await client.DisconnectAsync(true, timeoutSource.Token);
    }

    private MimeEntity BuildBody(EventCreatedNotification notification)
    {
        var localDate = TimeZoneInfo.ConvertTime(notification.Date, ResolveTimeZone(_options.TimeZone));
        var dateText = localDate.ToString("f", CultureInfo.GetCultureInfo("es-PE"));
        var text = new StringBuilder()
            .AppendLine($"Nombre: {notification.Name}")
            .AppendLine($"Fecha: {dateText}")
            .AppendLine($"Lugar: {notification.Venue}")
            .AppendLine($"Estado: {notification.Status}")
            .AppendLine()
            .AppendLine("Zonas:");

        foreach (var zone in notification.Zones)
        {
            text.AppendLine($"- {zone.Name}: {zone.Price:N2}, capacidad {zone.Capacity:N0}");
        }

        text.AppendLine().AppendLine($"Correlation ID: {notification.CorrelationId}");

        var rows = string.Join(string.Empty, notification.Zones.Select(zone =>
            $"<tr><td>{WebUtility.HtmlEncode(zone.Name)}</td>" +
            $"<td>{zone.Price:N2}</td><td>{zone.Capacity:N0}</td></tr>"));

        var html = $"""
            <h1>Nuevo evento registrado</h1>
            <p><strong>Nombre:</strong> {WebUtility.HtmlEncode(notification.Name)}</p>
            <p><strong>Fecha:</strong> {WebUtility.HtmlEncode(dateText)}</p>
            <p><strong>Lugar:</strong> {WebUtility.HtmlEncode(notification.Venue)}</p>
            <p><strong>Estado:</strong> {WebUtility.HtmlEncode(notification.Status)}</p>
            <table>
              <thead><tr><th>Zona</th><th>Precio</th><th>Capacidad</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p><small>Correlation ID: {notification.CorrelationId}</small></p>
            """;

        return new BodyBuilder { TextBody = text.ToString(), HtmlBody = html }.ToMessageBody();
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
