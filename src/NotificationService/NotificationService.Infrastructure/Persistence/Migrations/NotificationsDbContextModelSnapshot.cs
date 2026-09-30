using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NotificationsDbContext))]
internal partial class NotificationsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.12")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("NotificationService.Domain.Notification", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            entity.Property<int>("Attempts")
                .HasColumnType("integer")
                .HasColumnName("attempts");

            entity.Property<string>("Channel")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("channel");

            entity.Property<Guid>("CorrelationId")
                .HasColumnType("uuid")
                .HasColumnName("correlation_id");

            entity.Property<Guid>("EventId")
                .HasColumnType("uuid")
                .HasColumnName("event_id");

            entity.Property<string>("EventName")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("character varying(150)")
                .HasColumnName("event_name");

            entity.Property<string>("LastError")
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("last_error");

            entity.Property<Guid>("MessageId")
                .HasColumnType("uuid")
                .HasColumnName("message_id");

            entity.Property<string>("MessageType")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("message_type");

            entity.Property<DateTimeOffset>("OccurredAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("occurred_at");

            entity.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("payload");

            entity.Property<string>("PayloadHash")
                .IsRequired()
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnType("character(64)")
                .HasColumnName("payload_hash");

            entity.Property<DateTimeOffset>("ReceivedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("received_at");

            entity.Property<DateTimeOffset?>("SentAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("sent_at");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("status");

            entity.HasKey("Id").HasName("pk_notifications");
            entity.HasIndex("EventId").HasDatabaseName("ix_notifications_event_id");
            entity.HasIndex("MessageId").IsUnique().HasDatabaseName("uq_notifications_message_id");
            entity.HasIndex("Status")
                .HasFilter("status <> 'Sent'")
                .HasDatabaseName("ix_notifications_status");

            entity.ToTable("notifications", table =>
                table.HasCheckConstraint(
                    "ck_notifications_status",
                    "status IN ('Pending', 'Sent', 'Failed')"));
        });
#pragma warning restore 612, 618
    }
}
