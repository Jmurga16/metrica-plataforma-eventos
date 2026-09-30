using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace NotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NotificationsDbContext))]
[Migration("20260930000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                message_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                payload_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                attempts = table.Column<int>(type: "integer", nullable: false),
                last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notifications", x => x.id);
                table.CheckConstraint("ck_notifications_status", "status IN ('Pending', 'Sent', 'Failed')");
            });

        migrationBuilder.CreateIndex(
            name: "ix_notifications_event_id",
            table: "notifications",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_notifications_status",
            table: "notifications",
            column: "status",
            filter: "status <> 'Sent'");

        migrationBuilder.CreateIndex(
            name: "uq_notifications_message_id",
            table: "notifications",
            column: "message_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "notifications");
}
