using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXpProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProgressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalLifetimeXp = table.Column<long>(type: "bigint", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentEchelon = table.Column<int>(type: "integer", nullable: false),
                    DailyQuestXpToday = table.Column<int>(type: "integer", nullable: false),
                    DailyQuestXpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProgressions", x => x.Id);
                    table.CheckConstraint("CK_UserProgressions_CurrentLevel_AtLeastOne", "\"CurrentLevel\" >= 1");
                    table.CheckConstraint("CK_UserProgressions_DailyQuestXpToday_InRange", "\"DailyQuestXpToday\" >= 0 AND \"DailyQuestXpToday\" <= 500");
                    table.CheckConstraint("CK_UserProgressions_TotalLifetimeXp_NonNegative", "\"TotalLifetimeXp\" >= 0");
                    table.CheckConstraint("CK_UserProgressions_Version_NonNegative", "\"Version\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "XpTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    XpAmount = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProgressions_UserId",
                table: "UserProgressions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_UserId_BusinessDate",
                table: "XpTransactions",
                columns: new[] { "UserId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_UserId_IdempotencyKey",
                table: "XpTransactions",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_UserId_OccurredAtUtc",
                table: "XpTransactions",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_XpTransactions_UserId_Source",
                table: "XpTransactions",
                columns: new[] { "UserId", "Source" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProgressions");

            migrationBuilder.DropTable(
                name: "XpTransactions");
        }
    }
}
