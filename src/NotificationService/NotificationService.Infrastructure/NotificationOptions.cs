namespace NotificationService.Infrastructure;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; init; } = "localhost";
    public ushort Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string User { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public int[] RetryIntervalsMilliseconds { get; init; } = [];
}

public sealed class EmailOptions
{
    public const string SectionName = "Notifications:Email";
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public bool UseSsl { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string From { get; init; } = "events@localhost";
    public string To { get; init; } = string.Empty;
    public string TimeZone { get; init; } = "America/Lima";
    public int TimeoutSeconds { get; init; } = 10;
}
