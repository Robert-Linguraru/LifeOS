using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_DismissedRequiresRead", "\"DismissedAtUtc\" IS NULL OR \"ReadAtUtc\" IS NOT NULL");
                    table.CheckConstraint("CK_Notifications_SourcePair", "(\"SourceType\" IS NULL AND \"SourceId\" IS NULL) OR (\"SourceType\" IS NOT NULL AND \"SourceId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScheduledLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FiredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.CheckConstraint("CK_Reminders_Lifecycle", "((\"Status\" = 0 AND \"FiredAtUtc\" IS NULL AND \"NotificationId\" IS NULL) OR (\"Status\" = 1 AND \"FiredAtUtc\" IS NOT NULL AND \"NotificationId\" IS NOT NULL) OR (\"Status\" = 2 AND \"FiredAtUtc\" IS NULL AND \"NotificationId\" IS NULL))");
                    table.CheckConstraint("CK_Reminders_SourceShape", "((\"SourceType\" IN (0, 1) AND \"SourceId\" IS NOT NULL AND \"SourceTitle\" IS NOT NULL) OR (\"SourceType\" = 2 AND \"SourceId\" IS NULL AND \"SourceTitle\" IS NULL))");
                    table.CheckConstraint("CK_Reminders_Version_NonNegative", "\"Version\" >= 0");
                    table.ForeignKey(
                        name: "FK_Reminders_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_DismissedAtUtc_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "DismissedAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_DismissedAtUtc_ReadAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "DismissedAtUtc", "ReadAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IdempotencyKey",
                table: "Notifications",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_NotificationId",
                table: "Reminders",
                column: "NotificationId",
                unique: true,
                filter: "\"NotificationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_Status_ScheduledForUtc",
                table: "Reminders",
                columns: new[] { "Status", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId_IdempotencyKey",
                table: "Reminders",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId_Status_ScheduledForUtc",
                table: "Reminders",
                columns: new[] { "UserId", "Status", "ScheduledForUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
