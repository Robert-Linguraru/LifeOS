using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStrengthTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryMuscleGroup = table.Column<int>(type: "integer", nullable: false),
                    Equipment = table.Column<int>(type: "integer", nullable: false),
                    MovementPattern = table.Column<int>(type: "integer", nullable: false),
                    LoggingMode = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.CheckConstraint("CK_Exercises_Equipment", "\"Equipment\" BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_Exercises_LoggingMode", "\"LoggingMode\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_Exercises_MovementPattern", "\"MovementPattern\" BETWEEN 0 AND 11");
                    table.CheckConstraint("CK_Exercises_MuscleGroup", "\"PrimaryMuscleGroup\" BETWEEN 0 AND 11");
                    table.CheckConstraint("CK_Exercises_SortOrder_Positive", "\"SortOrder\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplates", x => x.Id);
                    table.UniqueConstraint("AK_WorkoutTemplates_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_WorkoutTemplates_Version_Positive", "\"Version\" >= 1");
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSecondaryMuscleGroups",
                columns: table => new
                {
                    MuscleGroup = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSecondaryMuscleGroups", x => new { x.ExerciseId, x.MuscleGroup });
                    table.CheckConstraint("CK_ExerciseSecondaryMuscleGroups_MuscleGroup", "\"MuscleGroup\" BETWEEN 0 AND 11");
                    table.ForeignKey(
                        name: "FK_ExerciseSecondaryMuscleGroups_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkoutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DiscardedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SessionFeeling = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RestTimerDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    RestTimerEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RestTimerPausedRemainingSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.UniqueConstraint("AK_WorkoutSessions_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_WorkoutSessions_LifecycleTimestamps", "(\"Status\" = 0 AND \"CompletedAtUtc\" IS NULL AND \"DiscardedAtUtc\" IS NULL) OR (\"Status\" = 1 AND \"CompletedAtUtc\" IS NOT NULL AND \"DiscardedAtUtc\" IS NULL) OR (\"Status\" = 2 AND \"CompletedAtUtc\" IS NULL AND \"DiscardedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_WorkoutSessions_RestTimerState", "(\"RestTimerDurationSeconds\" IS NULL AND \"RestTimerEndsAtUtc\" IS NULL AND \"RestTimerPausedRemainingSeconds\" IS NULL) OR (\"RestTimerDurationSeconds\" > 0 AND ((\"RestTimerEndsAtUtc\" IS NOT NULL AND \"RestTimerPausedRemainingSeconds\" IS NULL) OR (\"RestTimerEndsAtUtc\" IS NULL AND \"RestTimerPausedRemainingSeconds\" > 0)))");
                    table.CheckConstraint("CK_WorkoutSessions_SessionFeeling", "\"SessionFeeling\" IS NULL OR \"SessionFeeling\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_WorkoutSessions_Status", "\"Status\" BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_WorkoutSessions_Version_Positive", "\"Version\" >= 1");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutTemplates_OriginTemplateId",
                        column: x => x.OriginTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TargetSetCount = table.Column<int>(type: "integer", nullable: false),
                    TargetRepMin = table.Column<int>(type: "integer", nullable: true),
                    TargetRepMax = table.Column<int>(type: "integer", nullable: true),
                    DefaultRestSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercises", x => x.Id);
                    table.CheckConstraint("CK_WorkoutTemplateExercises_DefaultRestSeconds_NonNegative", "\"DefaultRestSeconds\" >= 0");
                    table.CheckConstraint("CK_WorkoutTemplateExercises_SortOrder_Positive", "\"SortOrder\" > 0");
                    table.CheckConstraint("CK_WorkoutTemplateExercises_TargetRepRange", "(\"TargetRepMin\" IS NULL AND \"TargetRepMax\" IS NULL) OR (\"TargetRepMin\" > 0 AND \"TargetRepMax\" > 0 AND \"TargetRepMin\" <= \"TargetRepMax\")");
                    table.CheckConstraint("CK_WorkoutTemplateExercises_TargetSetCount_Positive", "\"TargetSetCount\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_WorkoutTemplates_WorkoutTemplateId~",
                        columns: x => new { x.WorkoutTemplateId, x.UserId },
                        principalTable: "WorkoutTemplates",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessionExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    OriginalExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalExerciseNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LoggingModeSnapshot = table.Column<int>(type: "integer", nullable: false),
                    TargetSetCountSnapshot = table.Column<int>(type: "integer", nullable: false),
                    TargetRepMinSnapshot = table.Column<int>(type: "integer", nullable: true),
                    TargetRepMaxSnapshot = table.Column<int>(type: "integer", nullable: true),
                    DefaultRestSecondsSnapshot = table.Column<int>(type: "integer", nullable: false),
                    IsSkipped = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessionExercises", x => x.Id);
                    table.UniqueConstraint("AK_WorkoutSessionExercises_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_WorkoutSessionExercises_DefaultRestSeconds_NonNegative", "\"DefaultRestSecondsSnapshot\" >= 0");
                    table.CheckConstraint("CK_WorkoutSessionExercises_LoggingMode", "\"LoggingModeSnapshot\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_WorkoutSessionExercises_SortOrder_Positive", "\"SortOrder\" > 0");
                    table.CheckConstraint("CK_WorkoutSessionExercises_TargetRepRange", "(\"TargetRepMinSnapshot\" IS NULL AND \"TargetRepMaxSnapshot\" IS NULL) OR (\"TargetRepMinSnapshot\" > 0 AND \"TargetRepMaxSnapshot\" > 0 AND \"TargetRepMinSnapshot\" <= \"TargetRepMaxSnapshot\")");
                    table.CheckConstraint("CK_WorkoutSessionExercises_TargetSetCount_Positive", "\"TargetSetCountSnapshot\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkoutSessionExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSessionExercises_Exercises_OriginalExerciseId",
                        column: x => x.OriginalExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSessionExercises_WorkoutSessions_WorkoutSessionId_Us~",
                        columns: x => new { x.WorkoutSessionId, x.UserId },
                        principalTable: "WorkoutSessions",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Repetitions = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.Id);
                    table.CheckConstraint("CK_WorkoutSets_DurationSeconds_Positive", "\"DurationSeconds\" IS NULL OR \"DurationSeconds\" > 0");
                    table.CheckConstraint("CK_WorkoutSets_Kind", "\"Kind\" BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_WorkoutSets_Repetitions_Positive", "\"Repetitions\" IS NULL OR \"Repetitions\" > 0");
                    table.CheckConstraint("CK_WorkoutSets_SortOrder_Positive", "\"SortOrder\" > 0");
                    table.CheckConstraint("CK_WorkoutSets_Weight_Positive", "\"WeightKg\" IS NULL OR \"WeightKg\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutSessionExercises_WorkoutSessionExerciseI~",
                        columns: x => new { x.WorkoutSessionExerciseId, x.UserId },
                        principalTable: "WorkoutSessionExercises",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "CreatedAtUtc", "DeletedAtUtc", "Equipment", "IsActive", "IsDeleted", "LoggingMode", "MovementPattern", "Name", "PrimaryMuscleGroup", "SortOrder", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 0, "Barbell Bench Press", 0, 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 0, "Incline Barbell Bench Press", 0, 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 0, "Dumbbell Bench Press", 0, 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 0, "Incline Dumbbell Bench Press", 0, 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 0, "Machine Chest Press", 0, 5, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 0, "Cable Fly", 0, 6, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 0, "Push-Up", 0, 7, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 2, 0, "Weighted Push-Up", 0, 8, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 0, "Decline Bench Press", 0, 9, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000010"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 2, "Dumbbell Pullover", 0, 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000011"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 2, "Barbell Row", 1, 11, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000012"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 2, "Dumbbell Row", 1, 12, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000013"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 2, "Seated Cable Row", 1, 13, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000014"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 3, "Lat Pulldown", 1, 14, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000015"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 1, 3, "Pull-Up", 1, 15, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000016"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 1, 3, "Chin-Up", 1, 16, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000017"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 2, 3, "Weighted Pull-Up", 1, 17, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000018"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 3, 3, "Assisted Pull-Up", 1, 18, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000019"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 2, "Chest-Supported Row", 1, 19, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000020"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 2, "Single-Arm Cable Row", 1, 20, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000021"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 2, "Inverted Row", 1, 21, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000022"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 3, "Straight-Arm Pulldown", 1, 22, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000023"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 1, "Overhead Press", 2, 23, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000024"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 1, "Dumbbell Shoulder Press", 2, 24, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000025"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Lateral Raise", 2, 25, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000026"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 9, "Cable Lateral Raise", 2, 26, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000027"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Rear Delt Fly", 2, 27, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000028"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 2, "Face Pull", 2, 28, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000029"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 1, "Pike Push-Up", 2, 29, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000030"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 11, "Handstand Push-Up", 2, 30, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000031"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 1, "Arnold Press", 2, 31, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000032"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 4, "Back Squat", 6, 32, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000033"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 4, "Front Squat", 6, 33, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000034"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 4, "Goblet Squat", 6, 34, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000035"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 4, "Bodyweight Squat", 6, 35, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000036"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 4, "Leg Press", 6, 36, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000037"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 5, "Romanian Deadlift", 7, 37, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000038"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 5, "Conventional Deadlift", 7, 38, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000039"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 5, "Hip Thrust", 8, 39, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000040"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 6, "Bulgarian Split Squat", 6, 40, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000041"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 6, "Walking Lunge", 6, 41, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000042"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 6, "Reverse Lunge", 6, 42, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000043"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 6, "Step-Up", 6, 43, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000044"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 9, "Leg Extension", 6, 44, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000045"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 9, "Seated Leg Curl", 7, 45, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000046"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 9, "Standing Leg Curl", 7, 46, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000047"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 9, "Standing Calf Raise", 9, 47, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000048"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3, true, false, 0, 9, "Seated Calf Raise", 9, 48, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000049"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 8, true, false, 0, 4, "Kettlebell Goblet Squat", 6, 49, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000050"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 8, true, false, 0, 5, "Kettlebell Swing", 8, 50, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000051"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 5, "Good Morning", 7, 51, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000052"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 5, "Barbell Hip Thrust", 8, 52, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000053"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 9, "Bodyweight Calf Raise", 9, 53, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000054"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 4, "Cossack Squat", 6, 54, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000055"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 9, "Barbell Curl", 3, 55, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000056"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Dumbbell Curl", 3, 56, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000057"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Hammer Curl", 3, 57, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000058"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 9, "Cable Curl", 3, 58, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000059"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Incline Dumbbell Curl", 3, 59, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000060"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2, true, false, 0, 9, "Triceps Pushdown", 4, 60, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000061"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 9, "Skull Crusher", 4, 61, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000062"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Overhead Triceps Extension", 4, 62, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000063"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 7, true, false, 1, 1, "Dip", 4, 63, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000064"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 7, true, false, 2, 1, "Weighted Dip", 4, 64, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000065"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 7, true, false, 3, 1, "Assisted Dip", 4, 65, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000066"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 0, "Close-Grip Bench Press", 4, 66, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000067"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 0, 9, "Wrist Curl", 5, 67, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000068"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0, true, false, 0, 9, "Reverse Curl", 5, 68, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000069"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 1, 0, "Bodyweight Triceps Extension", 4, 69, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000070"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 1, 1, "Ring Dip", 4, 70, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000071"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 5, 8, "Plank", 10, 71, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000072"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 5, 8, "Side Plank", 10, 72, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000073"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 1, 8, "Hanging Knee Raise", 10, 73, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000074"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6, true, false, 1, 8, "Hanging Leg Raise", 10, 74, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000075"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 4, 8, "Ab Wheel Rollout", 10, 75, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000076"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 8, "Crunch", 10, 76, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000077"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 8, "Bicycle Crunch", 10, 77, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000078"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 7, true, false, 5, 11, "L-Sit", 10, 78, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000079"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 4, 8, "Russian Twist", 10, 79, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000080"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 8, "Dead Bug", 10, 80, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000081"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 6, 7, "Farmer's Carry", 11, 81, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000082"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1, true, false, 6, 7, "Suitcase Carry", 10, 82, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000083"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 5, 10, "Bear Crawl", 11, 83, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000084"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 10, "Burpee", 11, 84, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000085"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5, true, false, 4, 8, "Mountain Climber", 10, 85, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000086"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 8, true, false, 0, 10, "Kettlebell Clean", 11, 86, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000087"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 8, true, false, 0, 11, "Kettlebell Turkish Get-Up", 11, 87, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000088"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 9, true, false, 4, 2, "Resistance Band Pull-Apart", 2, 88, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000089"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 5, 10, "Battle Rope", 11, 89, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-4000-8000-000000000090"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10, true, false, 4, 11, "Box Jump", 6, 90, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "ExerciseSecondaryMuscleGroups",
                columns: new[] { "ExerciseId", "MuscleGroup" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000001"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000002"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000002"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000003"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000003"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000004"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000004"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000005"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000005"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000006"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000007"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000007"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000008"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000008"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000009"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000010"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000011"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000011"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000012"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000013"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000013"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000014"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000014"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000015"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000015"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000016"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000017"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000017"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000018"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000019"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000020"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000021"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000021"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000022"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000023"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000023"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000024"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000025"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000027"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000028"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000029"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000029"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000030"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000030"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000031"), 4 },
                    { new Guid("10000000-0000-4000-8000-000000000032"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000032"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000033"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000033"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000034"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000034"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000035"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000036"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000036"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000037"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000037"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000038"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000038"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000039"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000040"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000040"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000041"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000041"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000042"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000043"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000049"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000050"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000050"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000051"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000051"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000052"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000054"), 7 },
                    { new Guid("10000000-0000-4000-8000-000000000054"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000055"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000056"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000057"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000058"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000059"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000060"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000061"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000062"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000063"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000063"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000064"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000064"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000065"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000066"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000066"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000068"), 3 },
                    { new Guid("10000000-0000-4000-8000-000000000069"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000070"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000070"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000071"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000072"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000073"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000074"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000075"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000078"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000078"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000079"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000081"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000081"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000082"), 5 },
                    { new Guid("10000000-0000-4000-8000-000000000083"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000083"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000084"), 0 },
                    { new Guid("10000000-0000-4000-8000-000000000084"), 6 },
                    { new Guid("10000000-0000-4000-8000-000000000085"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000085"), 6 },
                    { new Guid("10000000-0000-4000-8000-000000000086"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000086"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000087"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000087"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000088"), 1 },
                    { new Guid("10000000-0000-4000-8000-000000000089"), 2 },
                    { new Guid("10000000-0000-4000-8000-000000000089"), 10 },
                    { new Guid("10000000-0000-4000-8000-000000000090"), 8 },
                    { new Guid("10000000-0000-4000-8000-000000000090"), 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_IsActive_SortOrder",
                table: "Exercises",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Name",
                table: "Exercises",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_ExerciseId",
                table: "WorkoutSessionExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_OriginalExerciseId",
                table: "WorkoutSessionExercises",
                column: "OriginalExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_UserId_WorkoutSessionId",
                table: "WorkoutSessionExercises",
                columns: new[] { "UserId", "WorkoutSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_WorkoutSessionId_SortOrder",
                table: "WorkoutSessionExercises",
                columns: new[] { "WorkoutSessionId", "SortOrder" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_WorkoutSessionId_UserId",
                table: "WorkoutSessionExercises",
                columns: new[] { "WorkoutSessionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_OriginTemplateId",
                table: "WorkoutSessions",
                column: "OriginTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 0 AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_Status_StartedAtUtc",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_WorkoutDate",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "WorkoutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_UserId_WorkoutSessionExerciseId",
                table: "WorkoutSets",
                columns: new[] { "UserId", "WorkoutSessionExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutSessionExerciseId_SortOrder",
                table: "WorkoutSets",
                columns: new[] { "WorkoutSessionExerciseId", "SortOrder" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutSessionExerciseId_UserId",
                table: "WorkoutSets",
                columns: new[] { "WorkoutSessionExerciseId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_ExerciseId",
                table: "WorkoutTemplateExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_UserId_WorkoutTemplateId",
                table: "WorkoutTemplateExercises",
                columns: new[] { "UserId", "WorkoutTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_WorkoutTemplateId_SortOrder",
                table: "WorkoutTemplateExercises",
                columns: new[] { "WorkoutTemplateId", "SortOrder" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_WorkoutTemplateId_UserId",
                table: "WorkoutTemplateExercises",
                columns: new[] { "WorkoutTemplateId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_UserId_IsDeleted",
                table: "WorkoutTemplates",
                columns: new[] { "UserId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseSecondaryMuscleGroups");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessionExercises");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "WorkoutTemplates");
        }
    }
}
