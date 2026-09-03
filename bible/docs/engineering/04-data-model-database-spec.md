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
- Future import metadata belongs to separately approved future module schemas;
  it is not part of M7 finance persistence.

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

Purpose: stable global reference categories for M7 transaction
classification. These are not personal user-owned financial records.

Fields:

- `Id`;
- no `UserId` for M7 global reference categories;
- `Name` required;
- `Type` enum: Income, Expense, Both;
- `SortOrder` int;
- `IsActive` bool;
- reference-data lifecycle fields as appropriate.

Constraints:

- unique `Name`;
- categories are immutable and not user-managed in M7.

Default V1 expense categories:

- Housing/Rent;
- Food;
- Transport;
- Fitness/Gym;
- Travel;
- Shopping;
- Entertainment;
- Bills/Utilities;
- Health;
- Other.

Default V1 income categories:

- Salary;
- Other Income.

### 4.9 FinanceTransaction

Purpose: manual income/expense tracking for V1.

Fields:

- `Id`;
- `UserId`;
- `TransactionDate` date-only;
- `Type` enum: Income, Expense;
- `Amount` decimal(18,2), positive only;
- `CategoryId` required;
- `Description` nullable;
- audit fields.

Constraints:

- amount must be greater than zero;
- category type must be compatible with transaction type;
- all M7-created records are manual by definition.

Indexes:

- `(UserId, TransactionDate)`;
- `(UserId, CategoryId, TransactionDate)`;
- `(UserId, Type, TransactionDate)`.

Finance calculations use transaction data only:

- monthly net cash flow = income transactions - expense transactions;
- yearly net cash flow = income transactions - expense transactions;
- transaction membership uses `TransactionDate`, never audit timestamps.

M7 does not contain `MonthlyFinancePlan`, planned income, allowance, expense
targets, persisted monthly/yearly totals, or aggregate correction records.

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
6. Finance reference categories, transactions, and currency preference.

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
- Finance monthly and yearly net cash flow must equal income transactions minus expense transactions.
- Deleted finance transactions must not affect ordinary summaries or dashboard counts.
- User progression must match XP transactions or be reconstructable.
- Soft-deleted records should not affect active dashboard counts unless explicitly included.

### 5.14 MealPrepPlan
### 5.15 MealPrepPlanItem
### 5.20 WeeklyIntention
## 5. Planned-domain data boundaries

No post-M7 entity has been implemented or approved as a final schema. `16-post-m7-domain-roadmap.md` is the canonical requirements source; each future milestone must define its concrete entities, relationships, constraints, lifecycle, and indexes before migration work begins.

M8 is defined in `16-m8-strength-training-architecture-contract.md`. It needs a shared seeded Exercise Library (including calisthenics), reusable Workout Templates, persisted Workout Sessions, ordered session exercises and sets, set kind, supported logging values, session feeling, and a simple Completed/Discarded lifecycle. Template changes must never rewrite completed session snapshots. It must not pre-add custom-exercise, RPE/RIR, program, device-import, or generic metric schemas.

M9 adds separate authoritative records appropriate to running/sprints/intervals, walks, daily steps, sports, and other activities. Unified Fitness history is a read projection—not a duplicate timeline table. PRs and Fitness Goals derive from historical evidence and must be recalculable; no mutable authoritative current-PR field is sufficient.

M10 separates reusable Food definitions (seeded and user custom) from dated historical Food Log Entries. Reusable meals expand into constituent historical entries. Nutrition targets, hydration increments, and simple supplement Taken/Not Taken state are required. Do not add external food IDs, barcode, provenance, recipe-platform, or configurable conversion schemas.

M11 records manual dated body-weight, sleep, and structured wellbeing values. Weight is Health-owned. Do not add generic health-metric, medical, or device-import schemas.

M12 adds native Calendar Events only. Tasks, Habits, Fitness, Nutrition, and Health remain in their domains and are projected into Calendar without duplicated Calendar records. Finance transactions are not projected.

M14 Analytics and future AI/integrations require separate architecture decisions. Do not reserve AI, import, external-ID, synchronization, or deduplication persistence in M8–M12.

## Database Constraints & Indexing

### Unique Constraints

The following uniqueness rules apply:

| Entity | Constraint |
|---------|------------|
| User | Email |
| FinanceCategory | Name |
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
| Finance Category | Global immutable reference data; no user-facing archive operation in M7. |
| User | All user-owned data follows the account deletion policy. |

### General Rules

- Archived task records remain available through explicit archived-task views. Soft-deleted records are hidden by query filters where implemented.
- Historical records remain available for reporting and audit purposes.
- Relationships must remain valid after an entity is archived.
- Permanent deletion is reserved for maintenance or account removal operations.