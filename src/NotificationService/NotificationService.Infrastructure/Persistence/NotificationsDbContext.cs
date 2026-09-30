using Microsoft.EntityFrameworkCore;
using NotificationService.Application;
using NotificationService.Domain;
using Npgsql;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), INotificationUnitOfWork
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var notification = modelBuilder.Entity<Notification>();
        notification.ToTable("notifications", table =>
        {
            table.HasCheckConstraint("ck_notifications_status", "status IN ('Pending', 'Sent', 'Failed')");
        });

        notification.HasKey(x => x.Id).HasName("pk_notifications");
        notification.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        notification.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        notification.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(100).IsRequired();
        notification.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        notification.Property(x => x.EventName).HasColumnName("event_name").HasMaxLength(150).IsRequired();
        notification.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        notification.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();
        notification.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        notification.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        notification.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(20).IsRequired();
        notification.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        notification.Property(x => x.Attempts).HasColumnName("attempts").IsRequired();
        notification.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
        notification.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
        notification.Property(x => x.SentAt).HasColumnName("sent_at");

        notification.HasIndex(x => x.MessageId).IsUnique().HasDatabaseName("uq_notifications_message_id");
        notification.HasIndex(x => x.EventId).HasDatabaseName("ix_notifications_event_id");
        notification.HasIndex(x => x.Status)
            .HasFilter("status <> 'Sent'")
            .HasDatabaseName("ix_notifications_status");
    }

    async Task INotificationUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "uq_notifications_message_id"
            })
        {
            var messageId = ChangeTracker.Entries<Notification>()
                .Select(entry => entry.Entity.MessageId)
                .FirstOrDefault();
            throw new DuplicateMessageException(messageId, exception);
        }
    }

    public void ClearTracking() => ChangeTracker.Clear();
}
