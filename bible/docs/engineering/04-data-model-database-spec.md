# 04 - Data Model / Database Specification

## 1. Purpose

This document defines the database shape for LifeOS. It contains:

- V1 schema specification;
- V1 constraints and indexes;
- future module schema appendix preserving the full product vision.

The future schema is not a command to build everything now. It is a reference so that V1 decisions do not block later modules.

## 2. Data modeling principles

- Use Guid IDs for application users and domain entities.
- Every personal entity has `UserId`.
- Store true instants in UTC.
- Store calendar-only values as date-only values.
- Use decimal for money.
- Use database constraints for uniqueness and integrity.
- Do not rely only on UI guards for important business rules.
- Keep XP append-only through `XPTransaction`.
- Keep import metadata when future imports are added.
- Preserve source of data where manual and imported records may coexist.

### 2.1 PostgreSQL type conventions

| Concept | .NET type | PostgreSQL type |
|---|---|---|
| Entity ID | Guid | uuid |
| User ID | Guid | uuid |
| True instant | DateTimeOffset | timestamp with time zone |
| Calendar date | DateOnly | date |
| Local time-of-day | TimeOnly | time without time zone |
| Money | decimal | numeric(18,2) |
| Long text | string | text |

Avoid global timestamp compatibility switches as a permanent solution.

## 3. Shared entities and types

### 3.1 BaseEntity

Fields:

- `Id` - primary key, Guid.
- `CreatedAtUtc` - UTC instant.
- `UpdatedAtUtc` - UTC instant.
- `IsDeleted` - bool.
- `DeletedAtUtc` - nullable UTC instant.

### 3.2 UserOwnedEntity

Fields:

- all `BaseEntity` fields;
- `UserId` - required Guid foreign key to `ApplicationUser`.

### 3.3 ApplicationUser

Future Identity integration will use Guid keys. `ApplicationUser` is not part of the current implementation and must not contain user-preference fields.

Fields:

- inherits from `IdentityUser<Guid>`;
- `DisplayName`;
- identity/account fields only as decided with the Identity milestone.

Constraints:

- Identity uniqueness constraints for email/username.

### 3.4 UserSettings

`UserSettings` is a separate `UserOwnedEntity`, with exactly one row per user enforced by a unique `UserId` constraint. It stores `TimeZoneId` (default `UTC`) and, for Milestone 6, `DateTimeOffset? TimeZoneConfiguredAtUtc`; only the latter proves explicit confirmation. Future preferences, such as currency or theme, belong here rather than on `ApplicationUser`.

`UserSettings` has no independent application-level delete lifecycle. Settings must not be reset or removed by deleting the row. Default settings are created only when a user genuinely has no settings row. Future account deletion may handle settings as part of the user/account lifecycle, but that is outside the current scope. Generic soft-delete infrastructure remains applicable to entities with a valid independent delete lifecycle.

## 4. V1 entities

### 4.1 TaskItem

Purpose: one-time task tracking.

Fields:

- `Id`;
- `UserId`;
- `Title` required, maximum 200 characters;
- `Description` nullable, maximum 2,000 characters;
- `DueDate` nullable date-only;
- `DueTime` nullable time-only;
- `Priority` enum: Low, Medium, High, Critical;
- `Status` enum: `TaskItemStatus` (Active = 0, Completed = 1, Archived = 2);
- `Category` enum: Personal, School, Health, Finance, Admin, Work, Fitness, Miscellaneous;
- `EstimatedTime` enum: Under15Minutes, Between15And30Minutes, Between30And60Minutes, Over60Minutes;
- `FrictionLevel` enum: Low, Medium, High;
- `CompletedAtUtc` nullable UTC instant;
- `CompletedDate` nullable date-only in the user's local time zone;
- audit fields.

Indexes:

- `(UserId, Status)`;
- `(UserId, DueDate)`;
- `(UserId, IsDeleted)`.

Notes:

- Due date and due time are planning fields in the user's local time zone.
- DueTime requires DueDate. Past due dates are valid and represent overdue tasks; DueTime is for display and sorting only.
- Reminder delivery is a later Milestone 6 concern and is not represented by task due-time shortcuts in Milestone 3.
- Recurrence fields are future scope.
- Snooze fields are future scope.

### 4.2 Habit

Purpose: recurring behavior definition.

Fields:

- `Id`;
- `UserId`;
- `Name` required, maximum 200 characters;
- `Description` nullable, maximum 2,000 characters;
- `Frequency` enum: Daily;
- `TargetType` enum: Binary, Quantity;
- `TargetQuantity` nullable decimal, precision 18, scale 2;
- `TargetUnit` nullable string, maximum 50 characters;
- `IsActive` bool;
- `EstimatedTime` enum;
- `FrictionLevel` enum;
- audit fields.

Indexes:

- `(UserId, IsActive)`;
- `(UserId, IsDeleted)`.

Notes:

- Milestone 4 accepts `Daily` as the only frequency. Other stable enum values are reserved for future behavior.
- New habits begin active. Archiving sets `IsActive = false`; archived habits remain persisted, are read-only, and cannot be completed. Milestone 4 has no restore/reactivate or user-facing delete/soft-delete operation.
- `TargetType = Quantity` describes optional goal metadata only. Completion remains binary and does not record an achieved quantity.
- Habit names are not unique per user in Milestone 4; names may be reused, including after archiving.
- Selected-day, weekly, monthly, and multiple-completion behavior are future scope.

### 4.3 HabitLog

Purpose: habit completion event.

Fields:

- `Id`;
- `UserId`;
- `HabitId`;
- `CompletionDate` date-only in the user's local time zone;
- `CompletedAtUtc` UTC instant;
- audit fields.

Constraints:

- unique `(UserId, HabitId, CompletionDate)` for Milestone 4. Duplicate completion calls are idempotent success; a concurrent uniqueness conflict resolves to the authoritative completed state.

Indexes:

- `(UserId, CompletionDate)`;
- `(HabitId, CompletionDate)`.

### 4.4 Reminder

Purpose: schedule a future in-app notification.

Fields are the inherited `UserOwnedEntity` fields plus required `SourceType`,
`Title` (max 200), `ScheduledLocalDate`, `ScheduledLocalTime` (minute precision),
`TimeZoneId` (max 100), `ScheduledForUtc`, `Status` (Pending by default),
`IdempotencyKey` (max 200), and `Version` (long, default 0). `SourceId` is
required for Task/Habit and null for Custom; `SourceTitle` is required for
Task/Habit and max 200; `Message` is optional and max 2000; `FiredAtUtc` and
`NotificationId` are required only for Fired.

Indexes:

- unique `(UserId, IdempotencyKey)`;
- `(UserId, Status, ScheduledForUtc)`;
- `(Status, ScheduledForUtc)` for worker discovery;
- unique filtered non-null `NotificationId`.

Constraints:

- Task/Habit requires source ID and title; Custom requires null source ID;
  Pending/Cancelled require null firing fields; Fired requires both; `Version >= 0`.
- Optional `NotificationId -> Notifications.Id` uses Restrict delete behavior.

Recurring reminders, snooze, parent reminders, and delivery attempts are outside
Milestone 6 and are not schema fields.

### 4.5 Notification

Purpose: in-app message to user.

Fields:

- `Id`;
- `UserId`;
Fields are the inherited `UserOwnedEntity` fields plus required `Type`, `Title`
(max 200), `Message` (max 2000), required `IdempotencyKey` (max 200), optional
paired `SourceType`/`SourceId`, nullable `ReadAtUtc`, and nullable
`DismissedAtUtc`. There is no persisted `IsRead`.

Indexes:

- `(UserId, DismissedAtUtc, CreatedAtUtc)`;
- `(UserId, DismissedAtUtc, ReadAtUtc)`.

Constraints:

- unique `(UserId, IdempotencyKey)`; SourceType and SourceId are both null or
  both non-null; dismissal requires a non-null ReadAtUtc. No public delete or
  undismiss behavior exists in Milestone 6.

### 4.6 XpTransaction

Purpose: append-only XP audit log.

Fields:

- `Id`;
- `UserId`;
- `Source` enum: `QuestCompletion`, `DailyScore`, `StreakBonus`, `ManualAdjustment`, `System`;
- `SourceType` nullable enum: Task, Habit, DailyScore, Streak;
- `SourceEntityId` nullable;
- `XpAmount` int;
- `OccurredAtUtc` UTC instant;
- `BusinessDate` date-only in the user's local time zone;
- `Notes` nullable;
- `IdempotencyKey` nullable string, max 200 characters;
- `Notes` nullable string, max 500 characters;
- audit fields.

Constraints:

- unique `(UserId, IdempotencyKey)` where `IdempotencyKey` is not null.

Indexes:

- `(UserId, OccurredAtUtc)`;
- `(UserId, BusinessDate)`;
- `(UserId, Source)`.

Idempotency key examples:

- `TaskComplete:{TaskId:D}`;
- `HabitComplete:{HabitId:D}:{CompletionDate:yyyy-MM-dd}`.

Milestone 5 Quest-completion awards require a non-null idempotency key. The unique index is `(UserId, IdempotencyKey)` for non-null keys. `XpAmount` is the actual positive capped award, not raw XP. Existing rows are append-only; persistence rejects modification or deletion before generic soft-delete conversion. No Task/Habit polymorphic foreign key is created, and source archive or soft deletion never removes or reverses XP. Future compensating transactions remain schema-compatible.

### 4.7 UserProgression

Purpose: denormalized current progression state.

Fields:

- inherited `Id` primary key and required unique `UserId`;
- `TotalLifetimeXP` long;
- `CurrentLevel` int;
- `CurrentEchelon` enum: Iron, Bronze, Silver, Gold, Platinum, Onyx, Radiant, Apex, Celestial, Immortal, Abyssal, Ascendant;
- `DailyQuestXPToday` int;
- `DailyQuestXPDate` nullable `DateOnly?`, mapped to PostgreSQL `date`;
- `Version` long, default 0, non-negative, and an EF concurrency token;
- `UpdatedAtUtc`.

Constraints:

- unique `UserId`.

Notes:

- Total lifetime XP should be long, not int.
- Level is derived from the documented level formula.
- Echelon is derived from documented level thresholds.
- The progression update and XP transaction creation must happen atomically.

Progression defaults are lifetime XP 0, level 1, echelon Iron, daily Quest XP 0, daily date null, and version 0. It is initialized lazily and race-safely on first access or award; no startup or user-seeding pipeline is added. The ledger sum for the target user-local `BusinessDate` is authoritative for the 500-XP cap, not the cache fields. `IXpRepository` is the single persistence boundary for XP transactions and progression; no separate progression repository or Unit of Work is required.

### 4.8 FinanceCategory

Purpose: finance categorization.

Fields:

- `Id`;
- `UserId` nullable if system default category;
- `Name` required;
- `Type` enum: Income, Expense, Both;
- `IsSystemDefault` bool;
- `SortOrder` int;
- `IsActive` bool;
- audit fields.

Constraints:

- unique `(UserId, Name)` for user categories;
- unique `(Name)` for system defaults if separated.

Default V1 expense categories:

- Rent;
- Food and groceries;
- Transport;
- Subscriptions;
- Going out/social;
- Clothes;
- Gym and fitness;
- Personal care;
- School/study;
- Miscellaneous.

Default V1 income categories:

- Allowance;
- Gift;
- Refund;
- Side income;
- Other income.

### 4.9 FinanceTransaction

Purpose: manual income/expense tracking for V1.

Fields:

- `Id`;
- `UserId`;
- `TransactionDate` date-only;
- `Type` enum: Income, Expense;
- `Amount` decimal(18,2), positive only;
- `Currency` string;
- `CategoryId` required;
- `Description` nullable;
- `Source` enum: Manual, FutureImport;
- `Notes` nullable;
- audit fields.

Constraints:

- amount must be greater than zero;
- source must be Manual for V1-created records.

Indexes:

- `(UserId, TransactionDate)`;
- `(UserId, CategoryId, TransactionDate)`;
- `(UserId, Type, TransactionDate)`.

### 4.10 MonthlyFinancePlan

Purpose: simple monthly allowance or planned monthly income.

Fields:

- `Id`;
- `UserId`;
- `Month` date-only as first day of month;
- `PlannedIncomeAmount` decimal(18,2) nullable;
- `ExpenseTarget` decimal(18,2) nullable;
- `Currency` string;
- audit fields.

Constraints:

- unique `(UserId, Month)`.

Finance formula:

- remaining planned balance = planned income amount + income transactions - expense transactions.
- The monthly allowance should not be entered twice as both planned income and an income transaction unless the product decision changes.

### 4.11 DailyScore - future, not V1

Purpose: daily score record for a later scoring engine. Do not implement this table in V1 unless a separate decision is made after V1 core usage.

Fields:

- `Id`;
- `UserId`;
- `ScoreDate` date-only;
- `HabitScore` nullable int;
- `TaskScore` nullable int;
- `SleepScore` nullable int;
- `WorkoutScore` nullable int;
- `FinanceScore` nullable int;
- `NutritionScore` nullable int;
- `WellbeingScore` nullable int;
- `TotalScore` int;
- `XPAwarded` int;
- audit fields.

Constraints:

- unique `(UserId, ScoreDate)`.

Indexes:

- `(UserId, ScoreDate)`.

Notes:

- When implemented later, do not create false zeros for modules not configured. Use nullable sub-scores or an excluded denominator model.

## 4.12 V1 migration order

Recommended migration sequence:

1. Base entities, UserSettings, and DbContext configuration.
2. Tasks.
3. Habits and HabitLogs with unique constraint.
4. XPTransaction and UserProgression.
5. Notifications and Reminders.
6. Finance categories, transactions, monthly plan.

## 4.13 Seed data

V1 seed data should include:

- single development user;
- user progression record;
- default finance categories;
- optional sample tasks/habits only in development mode.

Seed behavior must be idempotent.

## 4.14 Data integrity rules

- Habit completion must be duplicate-safe.
- XP award must be idempotent.
- Reminder firing must be idempotent.
- Finance monthly summaries must use transaction dates, not created dates.
- Finance remaining balance must not double-count planned allowance/income and income transactions.
- User progression must match XP transactions or be reconstructable.
- Soft-deleted records should not affect active dashboard counts unless explicitly included.

## 5. Future module schema appendix

The following entities preserve the product vision and should be added only when their module enters active development.

### 5.1 SleepEntry

Fields:

- `Id`;
- `UserId`;
- `SleepDate` date-only;
- `BedtimeUtc` nullable;
- `WakeTimeUtc` nullable;
- `DurationMinutes` nullable;
- `SleepQuality` nullable int 1-5;
- `EnergyOnWake` nullable int 1-5;
- `CaffeineAfterNoon` nullable bool;
- `Source` enum: Manual, GarminImport, OtherImport;
- `ImportBatchId` nullable;
- `Notes` nullable;
- audit fields.

Constraints:

- normally unique `(UserId, SleepDate, Source)` unless multiple segments are supported.

### 5.2 HealthMetricEntry

Fields:

- `Id`;
- `UserId`;
- `MetricDate` date-only;
- `MetricType` string or enum;
- `NumericValue` nullable decimal;
- `TextValue` nullable;
- `Unit` nullable;
- `Source` enum;
- audit fields.

### 5.3 WorkoutPlan

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `IsActive`;
- `CreatedDate` date-only;
- audit fields.

Constraint:

- one active plan per user enforced by service or partial unique index.

### 5.4 WorkoutDay

Fields:

- `Id`;
- `UserId`;
- `PlanId`;
- `DayLabel` enum: Push, Pull, Legs, Upper, Lower, Rest, Custom;
- `SortOrder`;
- audit fields.

### 5.5 Exercise

Fields:

- `Id`;
- `UserId` nullable for system library exercises;
- `Name`;
- `PrimaryMuscle` enum;
- `SecondaryMuscles` optional relationship or string;
- `Equipment` enum: Barbell, Dumbbell, Cable, Bodyweight, Machine, Other;
- `IsCustom` bool;
- audit fields.

### 5.6 PlannedExercise

Fields:

- `Id`;
- `UserId`;
- `WorkoutDayId`;
- `ExerciseId`;
- `TargetSets`;
- `TargetReps`;
- `TargetWeightKg` nullable;
- `SortOrder`;
- audit fields.

### 5.7 WorkoutSession

Fields:

- `Id`;
- `UserId`;
- `PlanId` nullable;
- `WorkoutDayId` nullable;
- `StartTimeUtc`;
- `EndTimeUtc` nullable;
- `DurationMinutes` nullable;
- `TotalVolumeKg` decimal nullable;
- `Notes` nullable;
- `Source` enum: Manual, GarminImport, OtherImport;
- audit fields.

### 5.8 SessionSet

Fields:

- `Id`;
- `UserId`;
- `SessionId`;
- `ExerciseId`;
- `SetNumber`;
- `RepsCompleted` int nullable;
- `WeightUsedKg` decimal nullable;
- `Status` enum: Completed, Failed, Skipped;
- `IsPersonalRecord` bool;
- audit fields.

### 5.9 BodyMetricEntry

Fields:

- `Id`;
- `UserId`;
- `Date` date-only;
- `WeightKg` decimal nullable;
- `ChestCm` decimal nullable;
- `WaistCm` decimal nullable;
- `HipsCm` decimal nullable;
- `LeftArmCm` decimal nullable;
- `RightArmCm` decimal nullable;
- `LeftThighCm` decimal nullable;
- `RightThighCm` decimal nullable;
- `ShouldersCm` decimal nullable;
- `BodyFatPct` decimal nullable;
- `Phase` enum: Bulk, Cut, Maintain;
- `HasPhoto` bool;
- audit fields.

### 5.10 ProgressPhoto

Fields:

- `Id`;
- `UserId`;
- `BodyMetricEntryId` nullable;
- `PhotoDate` date-only;
- `StoragePath`;
- `Phase` enum nullable;
- `Notes` nullable;
- audit fields.

### 5.11 MealEntry

Fields:

- `Id`;
- `UserId`;
- `Date` date-only;
- `MealName`;
- `ProteinG` decimal nullable;
- `CarbsG` decimal nullable;
- `FatG` decimal nullable;
- `CaloriesKcal` int nullable;
- `WaterMl` int nullable;
- `TemplateId` nullable;
- audit fields.

### 5.12 MealTemplate

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `ProteinG` decimal nullable;
- `CarbsG` decimal nullable;
- `FatG` decimal nullable;
- `CaloriesKcal` int nullable;
- audit fields.

### 5.13 NutritionTarget

Fields:

- `UserId`;
- `DailyProteinG` int nullable;
- `DailyCalories` int nullable;
- `DailyWaterMl` int nullable;
- `UpdatedAtUtc`.

### 5.14 MealPrepPlan

Fields:

- `Id`;
- `UserId`;
- `WeekStartDate` date-only;
- `Name`;
- `Notes` nullable;
- audit fields.

### 5.15 MealPrepPlanItem

Fields:

- `Id`;
- `UserId`;
- `MealPrepPlanId`;
- `MealName`;
- `Portions` int;
- `TargetDates` string or child table;
- audit fields.

### 5.16 StudySubject

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `WeeklyTargetHours` decimal nullable;
- `IsActive` bool;
- audit fields.

### 5.17 Project

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `Description` nullable;
- `TechStack` nullable;
- `Status` enum: Idea, InProgress, Paused, Shipped;
- `GitHubUrl` nullable;
- `StartDate` date-only nullable;
- `EndDate` date-only nullable;
- audit fields.

### 5.18 FocusSession / StudySession

Fields:

- `Id`;
- `UserId`;
- `SubjectId` nullable;
- `ProjectId` nullable;
- `TaskId` nullable;
- `StartTimeUtc`;
- `EndTimeUtc` nullable;
- `DurationMinutes`;
- `Topic` nullable;
- `Method` enum: Pomodoro, DeepWork, Review, Lecture, Other;
- `Notes` nullable;
- audit fields.

### 5.19 DailyWellbeing

Fields:

- `Id`;
- `UserId`;
- `Date` date-only;
- `MoodScore` int 1-5;
- `EnergyScore` int 1-5;
- `StressScore` int 1-5;
- `JournalText` text nullable;
- `WhatDrainedMe` string nullable;
- audit fields.

Constraint:

- unique `(UserId, Date)`.

### 5.20 WeeklyIntention

Fields:

- `Id`;
- `UserId`;
- `WeekStartDate` date-only;
- `Priority1`;
- `Priority2`;
- `Priority3`;
- `AIReviewText` nullable;
- audit fields.

Constraint:

- unique `(UserId, WeekStartDate)`.

### 5.21 AIConversation

Fields:

- `Id`;
- `UserId`;
- `Title`;
- `CreatedAtUtc`;
- `UpdatedAtUtc`;
- audit fields.

### 5.22 AIMessage

Fields:

- `Id`;
- `UserId`;
- `ConversationId`;
- `Role` enum: User, Assistant, System, Tool;
- `Content`;
- `CreatedAtUtc`;
- `Model` nullable;
- `TokenCount` nullable;
- audit fields.

### 5.23 Insight

Fields:

- `Id`;
- `UserId`;
- `Type` enum: WeeklyReview, Anomaly, Suggestion, Correlation, FinanceSummary, PhysiqueReport;
- `Title`;
- `Body`;
- `Confidence` enum: Low, Medium, High;
- `PeriodStart` date-only nullable;
- `PeriodEnd` date-only nullable;
- `CreatedAtUtc`;
- `DismissedAtUtc` nullable;
- audit fields.

### 5.24 InsightSource

Fields:

- `Id`;
- `UserId`;
- `InsightId`;
- `SourceType`;
- `SourceId`;
- audit fields.

### 5.25 ImportBatch

Fields:

- `Id`;
- `UserId`;
- `Source` enum: RevolutCsv, RaiffeisenCsv, RaiffeisenXls, GarminCsv, Other;
- `OriginalFileName`;
- `ImportedAtUtc`;
- `Status` enum: Pending, Previewed, Confirmed, Failed;
- `RecordCount`;
- `ErrorMessage` nullable;
- audit fields.

### 5.26 Future advanced finance entities

#### Budget

Fields:

- `Id`;
- `UserId`;
- `CategoryId`;
- `Month` date-only;
- `MonthlyLimit` decimal;
- audit fields.

#### Subscription

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `MonthlyCost` decimal;
- `RenewalDate` date-only;
- `CategoryId` nullable;
- `IsWorthIt` nullable bool;
- audit fields.

#### SavingsGoal

Fields:

- `Id`;
- `UserId`;
- `Name`;
- `TargetAmount` decimal;
- `CurrentAmount` decimal;
- `TargetDate` date-only nullable;
- `Notes` nullable;
- audit fields.

#### NetWorthSnapshot

Fields:

- `Id`;
- `UserId`;
- `Month` date-only;
- `TotalAmount` decimal;
- `Notes` nullable;
- audit fields.

## Database Constraints & Indexing

### Unique Constraints

The following uniqueness rules apply:

| Entity | Constraint |
|---------|------------|
| User | Email |
| FinanceCategory | (UserId, Name) |
| UserSettings | UserId (one settings record per user) |

---

### Recommended Indexes

#### Task

- UserId
- DueDate
- Status
- Archived
- CreatedAt

#### Habit

- UserId
- IsActive
- IsDeleted

#### HabitLog

- HabitId
- Date

#### Reminder

- UserId
- ScheduledAt
- Status

#### Notification

- UserId
- Status
- SentAt

#### XPTransaction

- UserId
- CreatedAt

#### FinanceTransaction

- UserId
- Date
- CategoryId

#### MonthlyBudget

- UserId
- Month

--- 

### Notes

- Every foreign key should be indexed.
- Composite indexes should only be introduced when supported by application query patterns.
- Indexes should be reviewed as new features are introduced to avoid unnecessary write overhead.


## Archive & Soft-Delete Behavior

Archive and soft delete are distinct concepts. `AppDbContext` converts normal EF deletion of a `BaseEntity` into a soft delete. Records are not physically removed through normal user actions.

### Cascade Rules

| Entity | Cascade Behavior |
|---------|------------------|
| Task | Archiving changes `TaskItemStatus` to `Archived`; it does not set `IsDeleted`. Soft deletion sets `IsDeleted` and `DeletedAtUtc`. Associated reminder behavior is future Milestone 6 scope. |
| Habit | Archiving sets `IsActive = false`; the habit and its immutable logs remain persisted. Archived habits are read-only and cannot create new logs. User-facing habit deletion/soft deletion is not a Milestone 4 operation. |
| Reminder | Notification history is retained. |
| Finance Category | Cannot be archived while referenced by existing transactions. |
| User | All user-owned data follows the account deletion policy. |

### General Rules

- Archived task records remain available through explicit archived-task views. Soft-deleted records are hidden by query filters where implemented.
- Historical records remain available for reporting and audit purposes.
- Relationships must remain valid after an entity is archived.
- Permanent deletion is reserved for maintenance or account removal operations.