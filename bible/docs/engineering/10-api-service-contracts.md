# API / Service Contracts

---

# 1. Purpose

This document defines public service contracts used by LifeOS.

It represents the contract between the Presentation Layer and Core service contracts. Contracts and DTOs live in `LifeOS.Core`; Infrastructure provides EF-backed implementations. There is no separate Application project.

This document intentionally defines:

- service responsibilities;
- public methods;
- DTO ownership;
- validation ownership;
- business rule ownership;
- dependencies;
- return values;
- interaction rules.

This document intentionally does **not** define implementation details.

Implementation belongs in source code.

---

# 2. Service Design Philosophy

Every service exists to own a single business domain.

Examples

TaskService owns Tasks.

HabitService owns Habits.

FinanceService owns Finance.

XPService owns Progression.

DashboardService owns Dashboard aggregation.

Services should communicate with each other through interfaces.

The Presentation Layer must never bypass a service.

---

# 3. General Service Rules

Every service shall:

- expose business operations only;
- return DTOs rather than entities;
- perform validation before persistence;
- enforce business rules;
- remain user-scoped;
- throw the established application exceptions when required;
- never expose EF Core entities.

Services should be stateless.

Services should be deterministic whenever possible.

---

# 4. Naming Convention

All asynchronous methods follow the .NET convention.

```
MethodNameAsync()
```

Method names describe business intent.

Good

```
CreateTaskAsync()

CompleteHabitAsync()

AwardQuestXPAsync()

GetDashboardAsync()
```

Avoid

```
Save()

Execute()

Update()

Process()
```

unless their purpose is immediately obvious.

---

# 5. DTO Conventions

DTOs belong in `LifeOS.Core` with the service contracts they support.

Every feature should use the following DTO pattern.

Create DTO

```
CreateTaskDto
```

Update DTO

```
UpdateTaskDto
```

Summary DTO

```
TaskSummaryDto
```

Details DTO

```
TaskDetailsDto
```

Dashboard DTO

```
TaskDashboardDto
```

Statistics DTO

```
TaskStatisticsDto
```

This convention applies consistently across every module.

Examples

```
CreateHabitDto

HabitSummaryDto

FinanceSummaryDto

WorkoutSessionDetailsDto

StudyStatisticsDto
```

---

# 6. Validation Rules

Validation occurs in three stages.

Stage 1

Presentation

Basic client validation.

Examples

- required fields
- number ranges
- maximum length

Purpose

Improve UX.

Never trusted.

---

Stage 2

Service implementation

Business validation.

Examples

- duplicate habit
- invalid reminder
- invalid task state
- duplicate XP

This is the authoritative validation layer.

---

Stage 3

Database

Constraints.

Examples

- unique indexes
- foreign keys
- required relationships

Database constraints are the final safety net.

---

# 7. Error Handling

Business rule violations should use the existing `LifeOSException` hierarchy.

Examples

```
ValidationException

ResourceNotFoundException

CurrentUserUnavailableException
```

Unexpected failures should never expose raw infrastructure exceptions to the UI.

The Presentation Layer is responsible for displaying friendly messages.

Infrastructure exceptions remain logged.

---

# 8. Base Service Contract

Every service follows this structure.

Purpose

Responsibilities

Dependencies

Public API

Business Rules

Validation

Exceptions

Returns

Forbidden Responsibilities

Future Expansion

Every service in this document follows this format.

---

# 9. DashboardService

## Purpose

At Milestone 3, DashboardService aggregates task information into a DashboardDto.

DashboardService owns no business rules.

Its responsibility is orchestration only.

---

## Responsibilities

- Build DashboardDto
- Aggregate module summaries
- Coordinate dashboard widgets
- Return today's overview
- Keep dashboard queries centralized

---

## Dependencies

At Milestone 3, DashboardService may depend on

```
ITaskService
```

DashboardService must never access repositories directly.

---

## Public API

```csharp
Task<DashboardDto> GetDashboardAsync();

Task<DashboardTaskWidgetDto> GetTaskWidgetAsync();
```

---

## Business Rules

DashboardService

does NOT

- calculate XP
- calculate streaks
- calculate finance totals

It consumes already-calculated data.

---

## Returns

DashboardDto

Containing

- Today's Tasks
- Task summary
- Task quick action

Future module slices may extend DashboardDto only when their owning services exist.

---

## Forbidden Responsibilities

DashboardService must never

- write to the database;
- modify entities;
- award XP;
- schedule reminders;
- calculate finance;
- calculate streaks.

---

## Future Expansion

DashboardService may later aggregate

- Sleep
- Fitness
- Nutrition
- Body Metrics
- Study
- Projects
- Wellbeing
- AI Insights

without changing its architectural role.

---

# 10. SettingsService

## Purpose

Manage user configuration.

Settings affect application behaviour but are not business data.

---

## Responsibilities

- Retrieve user settings
- Update user settings
- Store user preferences
- Manage application configuration per user

`SettingsService` exposes no independent delete operation for `UserSettings`. Settings are one per user for the lifetime of that user; resetting preferences must not be implemented by deleting the settings row. Default settings are created only when no settings row genuinely exists.

---

## Dependencies

```
ISettingsRepository

ICurrentUserService
```

---

## Public API

```csharp
Task<UserSettingsDto> GetSettingsAsync();

Task UpdateSettingsAsync(UpdateUserSettingsDto dto);

Task UpdateThemeAsync(ThemePreference theme);

Task UpdateTimeZoneAsync(string timeZoneId);

Task UpdateCurrencyAsync(string currencyCode);

Task UpdateMonthlyAllowanceAsync(decimal allowance);

Task ResetSettingsAsync();
```

---

## Business Rules

Changing settings must never alter historical business data.

Changing timezone affects future calculations only.

Changing currency changes presentation, not stored monetary values.

---

## Returns

UserSettingsDto

Containing

- Theme
- TimeZone
- Currency
- Monthly Allowance
- Display Preferences

---

## Forbidden Responsibilities

SettingsService must never

- modify Tasks;
- modify Habits;
- modify XP;
- modify Finance Transactions.

It only modifies configuration.

---

# 11. Cross-Service Rules

Services communicate only when necessary.

Preferred direction

```
Presentation

↓

Application Services

↓

Repositories

↓

Database
```

Services should never form circular dependencies.

DashboardService is the only aggregation service.

XPService is the only progression service.

FinanceService is the only financial calculation service.

ReminderService is the only reminder scheduling service.

Business ownership must always remain obvious.

---

# 12. TaskService

## Purpose

TaskService owns the complete lifecycle of Tasks.

It is the only service responsible for creating, modifying, completing, archiving, deleting, and retrieving TaskItems.

Every Task-related business rule is enforced here.

TaskService acts as the boundary between the Presentation Layer and the Task domain.

---

# Responsibilities

TaskService is responsible for:

- Creating Tasks
- Updating Tasks
- Completing Tasks
- Archiving Tasks
- Soft deleting Tasks
- Retrieving Tasks
- Filtering Tasks
- Calculating Task statistics
- Validating Task ownership

TaskService is the only service allowed to modify TaskItem entities.

---

# Dependencies

TaskService depends on:

```text
ITaskRepository

ICurrentUserService

IDateTimeProvider

ILogger<TaskService>
```

TaskService does NOT depend on:

```text
DashboardService

FinanceService

WorkoutService

AIService
```

---

# Public API

## Create

```csharp
Task<Guid> CreateTaskAsync(CreateTaskDto dto);
```

Creates a new Task.

Returns

- Task Id

---

## Update

```csharp
Task UpdateTaskAsync(UpdateTaskDto dto);
```

Updates an existing Task.

---

## Complete

```csharp
Task CompleteTaskAsync(Guid taskId);
```

Marks a Task as completed. Completing an already-completed task is a successful no-op that preserves its original completion values.

---

## Archive

```csharp
Task ArchiveTaskAsync(Guid taskId);
```

Archives a Task.

Archived Tasks are hidden from active views.

---

## Delete

```csharp
Task DeleteTaskAsync(Guid taskId);
```

Soft deletes a Task.

Historical XP must remain unaffected.

---

## Get By Id

```csharp
Task<TaskDetailsDto?> GetTaskByIdAsync(Guid taskId);
```

Returns

TaskDetailsDto

or

null.

---

## Get Today

```csharp
Task<IEnumerable<TaskSummaryDto>> GetTodayTasksAsync();
```

Returns Tasks due today.

---

## Get Upcoming

```csharp
Task<IEnumerable<TaskSummaryDto>> GetUpcomingTasksAsync();
```

Returns future Tasks.

---

## Get Overdue

```csharp
Task<IEnumerable<TaskSummaryDto>> GetOverdueTasksAsync();
```

Returns overdue Tasks.

---

## Get Unscheduled

```csharp
Task<IEnumerable<TaskSummaryDto>> GetUnscheduledTasksAsync();
```

Returns active Tasks with no due date.

---

## Get Active

```csharp
Task<IEnumerable<TaskSummaryDto>> GetActiveTasksAsync();
```

Returns all active Tasks.

---

## Get Archived

```csharp
Task<IEnumerable<TaskSummaryDto>> GetArchivedTasksAsync();
```

Returns archived Tasks.

---

## Search

```csharp
Task<IEnumerable<TaskSummaryDto>> SearchTasksAsync(string searchTerm);
```

Searches Tasks belonging to the current user.

---

## Statistics

```csharp
Task<TaskStatisticsDto> GetTaskStatisticsAsync();
```

Returns:

- Total Tasks
- Active Tasks
- Completed Tasks
- Overdue Tasks
- Completion Percentage

---

# DTO Contracts

## Input DTOs

```text
CreateTaskDto

UpdateTaskDto
```

---

## Output DTOs

```text
TaskSummaryDto

TaskDetailsDto

TaskStatisticsDto

TaskDashboardDto
```

---

# Business Rules

TaskService owns the following rules.

## Creation

- Title is required.
- Due date is optional.
- Due time is optional.
- Estimated Time defaults to Under15Minutes.
- Friction defaults to Low.
- New Tasks begin as Active.

---

## Update

The current user must own the Task.

Completed and archived Tasks are read-only.

---

## Completion

Completing a Task:

- stores CompletedAtUtc;
- stores CompletedDate;
- changes Status to Completed;
Completing the same Task twice succeeds without changing `CompletedAtUtc` or `CompletedDate`.

---

## Archive

Archived Tasks:

- disappear from active views;
- remain in history;
- have `TaskItemStatus.Archived` and do not set `IsDeleted`.

---

## Delete

Delete performs a Soft Delete.

Delete never removes:

- existing historical records associated with the task.

Future administrative hard delete is outside V1 scope.

---

## Ownership

Every Task belongs to exactly one User.

Every Task operation validates ownership.

TaskService never exposes another user's Tasks.

---

# Validation Rules

TaskService validates:

- required Title;
- ownership;
- due dates;
- completion status;
- archive state;
- DueTime requires DueDate;
- title maximum of 200 characters;
- description maximum of 2,000 characters.

Validation occurs before persistence.

---

# Exception Contracts

TaskService may throw:

```text
ResourceNotFoundException

ValidationException

CurrentUserUnavailableException
```

Infrastructure exceptions should never escape directly.

---

## Future integrations

XP integration belongs to Milestone 5 and reminder integration belongs to Milestone 6. Milestone 3 does not depend on `IXPService`, `IReminderService`, feature flags, placeholder implementations, XP transactions, or reminder fields. Later slices may integrate through their own established services without changing the Task completion semantics.

---

# Repository Usage

TaskService retrieves persistence through:

```text
ITaskRepository
```

Allowed repository methods

```text
GetById

GetToday

GetUpcoming

GetOverdue

GetActive

GetArchived

Search

Add

Update

Delete
```

Repositories never contain business logic.

---

# Return Contracts

Create

Returns

```text
Guid
```

---

Update

Returns

```text
Task
```

---

Queries

Return

```text
DTOs only
```

Never entities.

---

# Logging

TaskService logs:

- Task Created
- Task Updated
- Task Completed
- Task Archived
- Task Deleted
- Validation failures
- Unexpected failures

Sensitive Task notes should never be written to logs.

---

# Performance Requirements

TaskService should:

- use asynchronous operations;
- avoid unnecessary database queries;
- retrieve only required fields;
- paginate future large result sets.

---

# Forbidden Responsibilities

TaskService must never:

- calculate XP;
- calculate Dashboard summaries;
- calculate Finance totals;
- access UI components;
- perform AI analysis;
- directly manipulate UserProgression.

---

# Future Expansion

Future versions may extend TaskService with:

- Recurring Tasks
- Snooze
- Reschedule
- Task Templates
- Task Attachments
- Calendar Integration
- Time Blocking
- AI Prioritisation
- Task Dependencies
- Bulk Operations

These features should extend the existing contract without changing current behaviour.

---

# 13. HabitService

## Purpose

HabitService owns the Milestone 4 lifecycle of Habits and immutable HabitLogs.

It defines daily habits, records one binary completion event per user-local date, calculates the basic current streak, and exposes habit history and dashboard data.

HabitService is the only service allowed to create HabitLogs.

---

# Responsibilities

HabitService is responsible for:

- Creating Habits
- Updating active Habits
- Archiving Habits
- Completing active daily Habits
- Creating immutable HabitLogs
- Preventing duplicate completions idempotently
- Calculating the current daily streak
- Retrieving newest-first history
- Returning dashboard data through DTOs

---

# Dependencies

HabitService depends on

```text
IHabitRepository

ICurrentUserService

IUserSettingsService

IDateTimeProvider

ILogger<HabitService>
```

HabitService must NOT depend on

```text
DashboardService

FinanceService

IXPService

IReminderService

Notification services
```

XP integration belongs to Milestone 5. Reminder and notification integration belongs to Milestone 6.

---

# Public API

## Create

```csharp
Task<HabitDetailsDto> CreateHabitAsync(
    CreateHabitDto dto,
    CancellationToken cancellationToken = default);
```

Creates an active daily Habit for the current user.

## Update

```csharp
Task<HabitDetailsDto> UpdateHabitAsync(
    Guid habitId,
    UpdateHabitDto dto,
    CancellationToken cancellationToken = default);
```

Updates an active Habit. Archived Habits are read-only.

## Get By Id

```csharp
Task<HabitDetailsDto> GetHabitByIdAsync(
    Guid habitId,
    CancellationToken cancellationToken = default);
```

## List

```csharp
Task<HabitListDto> GetHabitListAsync(
    CancellationToken cancellationToken = default);
```

Returns the current user's active and archived Habit views as defined by the UI contract.

## Complete Today

```csharp
Task<HabitDetailsDto> CompleteHabitTodayAsync(
    Guid habitId,
    CancellationToken cancellationToken = default);
```

Completes an active daily Habit for the current user's local date. A duplicate call is a successful no-op and returns the authoritative completed state.

## Archive

```csharp
Task<HabitDetailsDto> ArchiveHabitAsync(
    Guid habitId,
    CancellationToken cancellationToken = default);
```

Sets `IsActive = false`. Archived Habits remain persisted, are read-only, and cannot be completed. Milestone 4 exposes no restore/reactivate or delete operations.

## History

```csharp
Task<IReadOnlyList<HabitLogDto>> GetHabitHistoryAsync(
    Guid habitId,
    CancellationToken cancellationToken = default);
```

Returns immutable completion logs for one user-owned Habit, ordered newest-first. History is available for active and archived Habits.

## Current Streak

```csharp
Task<int> GetCurrentStreakAsync(
    Guid habitId,
    CancellationToken cancellationToken = default);
```

Uses distinct local completion dates. If today is complete it anchors today; otherwise yesterday may anchor the streak. If neither is complete the result is zero. It walks backward through consecutive dates, stops at the first missing date, and ignores future dates.

---

# DTO Contracts

Input DTOs

```text
CreateHabitDto

UpdateHabitDto
```

Output DTOs

```text
HabitSummaryDto

HabitDetailsDto

HabitListDto

HabitLogDto
```

---

# Business Rules

## Creation

- Name is required.
- Name is limited to 200 characters.
- Description is optional and limited to 2,000 characters.
- Frequency defaults to Daily and only Daily is accepted in Milestone 4.
- TargetType defaults to Binary.
- Target quantity and unit are optional definition metadata.
- TargetUnit is optional and limited to 50 characters.
- EstimatedTime defaults to Under15Minutes.
- Friction defaults to Low.
- New Habits begin active.
- Habit names are not unique per user.

## Update and archive

- Only the current user's active Habit can be updated.
- Archiving sets `IsActive = false`.
- Archived Habits remain persisted and available for history.
- Archived Habits are read-only and cannot be completed.
- Milestone 4 has no restore/reactivate or user-facing delete/soft-delete operation.

## Completion

- Completion is binary, even when the Habit definition has a quantity target.
- Users do not enter an achieved quantity during completion.
- Completing an active daily Habit creates exactly one immutable HabitLog.
- The log records `UserId`, `HabitId`, the user's local `CompletionDate`, and the UTC completion instant.
- The database enforces uniqueness on `(UserId, HabitId, CompletionDate)`.
- Completing the same Habit more than once for the same local day is a successful no-op. The original log remains authoritative and no second log is created.
- Concurrent duplicate requests resolve to the authoritative completed state rather than becoming a normal user-visible duplicate-completion exception.

## Streak rules

- Use distinct local `CompletionDate` values.
- If today is complete, anchor the streak on today.
- Otherwise, if yesterday is complete, anchor the streak on yesterday.
- Otherwise, current streak is zero.
- Walk backward through consecutive local dates and stop at the first missing date.
- Future dates do not contribute.
- Weekly streaks, momentum streaks, streak freezes, grace days, and best-streak statistics are outside Milestone 4.

## Ownership

Every Habit and HabitLog belongs to exactly one user. Every service operation obtains or validates the current user through `ICurrentUserService`.

---

# Validation Rules

HabitService validates:

- required name;
- current-user availability and ownership;
- Daily-only frequency;
- valid target type;
- valid quantity target metadata;
- active-only update and completion;
- archive lifecycle state.

---

# Exception Contracts

```text
ResourceNotFoundException

ValidationException

CurrentUserUnavailableException
```

Milestone 4 does not require a Habit-specific exception hierarchy.

---

# Later Integrations

Habit completion is intentionally a clean integration point for later milestones. XP belongs to Milestone 5; reminders, notifications, and background scheduling belong to Milestone 6. None are Milestone 4 HabitService dependencies or acceptance requirements.

---

# Repository Usage

Allowed

```text
Add

Update

Archive

GetById

GetList

GetHistory

AddCompletion

GetCurrentStreak
```

Repositories perform persistence and query execution only. They do not calculate streaks or own Habit business workflows.

---

# Return Contracts

Queries return DTOs or read-only DTO collections. Mutations return DTOs. Habit entities are never exposed to Web.

---

# Logging

Log useful lifecycle events such as:

- Habit Created
- Habit Updated
- Habit Completed
- Habit Archived
- Unexpected Failure

Do not log private Habit notes or sensitive user data.

---

# Forbidden Responsibilities

HabitService must never:

- award XP in Milestone 4;
- schedule reminders or create notifications;
- calculate finance;
- access UI components;
- perform AI analysis.

---

# Future Expansion

Future versions may add:

- Selected-day, weekly, and monthly schedules;
- multiple daily completions;
- restore/reactivate if separately approved;
- quantity achievement entry;
- Habit templates;
- Habit categories;
- XP integration in Milestone 5;
- reminder integration in Milestone 6;
- advanced statistics and streak features.

---

# 14. XpService

## Purpose

`XpService` owns the Milestone 5 XP/progression core vertical slice.

Every XP change in the application must pass through XPService.

No other service may directly modify UserProgression or XPTransaction.

---

# Responsibilities

`XpService` is responsible for

- Awarding XP
- Enforcing Daily XP Cap
- Creating XP Transactions
- Updating User Progression
- Calculating Levels
- Calculating Echelons
- Returning Progression information
- Returning level/echelon transition metadata and logging significant transitions where appropriate

---

# Dependencies

XPService depends on

```text
IXpRepository

IDateTimeProvider

ICurrentUserService

ILogger<XpService>
```

XPService must NOT depend on

```text
TaskRepository

HabitRepository

FinanceRepository

DashboardService
```

XPService receives events.

It never retrieves Task or Habit data itself.

---

# Public API

## Award Quest XP

```csharp
Task<XpAwardResultDto> AwardQuestXpAsync(...);
```

---

## Get Progression

```csharp
Task<UserProgressionDto> GetProgressionAsync();
```

---

## Get XP History

```csharp
Task<IEnumerable<XpTransactionDto>> GetXpHistoryAsync();
```

---

## Calculate Quest XP

```csharp
Task<int> CalculateQuestXpAsync(
    EstimatedTime estimatedTime,
    FrictionLevel friction);
```

---

## Calculate Level

```csharp
Task<int> CalculateLevelAsync(long totalXp);
```

---

## Calculate Echelon

```csharp
Task<Echelon> CalculateEchelonAsync(int level);
```

---

## Check Daily Cap

```csharp
Task<bool> CanAwardQuestXpAsync();
```

---

# DTO Contracts

Input

```text
AwardQuestXpDto

AwardDailyScoreDto

AwardStreakBonusDto
```

Output

```text
UserProgressionDto

XPTransactionDto

XPStatisticsDto
```

---

# Business Rules

XPService owns

- Quest XP Formula
- Daily XP Cap
- XP Transactions
- Level Formula
- Echelon Formula

---

## Daily Cap

Maximum

```
500 Quest XP
```

per local day.

Cap enforcement occurs before creating XPTransaction.

---

## Transactions

Every positive actual award creates one `XpTransaction`; a zero actual award creates none. `XpAmount` is the capped awarded amount, not raw XP.

Every XPTransaction has

- User
- Source
- Source Entity
- Amount
- Timestamp
- Business Date
- Idempotency Key

---

## Progression

Progression updates atomically with XPTransaction.

Either both succeed

or

both fail.

---

## Idempotency

Awarding XP twice for the same completion is forbidden.

Duplicate requests must return safely.

---

# Validation Rules

XPService validates

- Daily Cap
- Duplicate Idempotency Key
- Positive XP
- Valid Source
- Current-user validation and lazy, race-safe progression initialization

---

# Exception Contracts

```text
DailyQuestCapReachedException

DuplicateXPTransactionException

ProgressionNotFoundException

InvalidXPSourceException
```

---

# Notification Integration

Committed level and echelon transitions create the required progression
notifications inside the existing XP aggregate transaction. They are not
best-effort post-XP calls through `INotificationService`; duplicate XP requests
create no duplicate notifications.

---

### Notification Delivery Policy

> Historical/non-authoritative: this prototype delivery policy is not part of Milestone 6. M6 notifications are in-app only and have the `Unread`, `Read`, and `Dismissed` user lifecycle. Sent, Delivered, Failed, and Expired are not persisted M6 states.

# Repository Usage

Allowed

```text
Add XPTransaction

Update UserProgression

Get History

Get Progression
```

Repositories never calculate XP.

---

# Return Contracts

Mutations return

```text
Task
```

Queries return

```text
DTOs
```

Never entities.

---

# Logging

Log

- XP Awarded
- XP Rejected
- Daily Cap Hit
- Level Up
- Echelon Change
- Unexpected Failure

---

# Forbidden Responsibilities

XPService must never

- query Tasks;
- query Habits;
- calculate Dashboard;
- calculate Finance;
- manipulate UI.

---

# Future Expansion

Future versions may extend XPService with

- Daily Score XP
- Streak Bonus XP
- Achievements
- Badges
- Seasonal Events
- Multipliers
- Bonus Quests
- AI Progress Reviews

---

# 15. ReminderService

## Purpose

ReminderService owns the complete reminder lifecycle within LifeOS.

It is responsible for scheduling reminders, validating reminder data, converting user-local times into UTC, processing due reminders, and coordinating notification creation.

ReminderService is the only service allowed to schedule or trigger reminders.

---

# Responsibilities

ReminderService is responsible for:

- Creating reminders
- Updating reminders
- Cancelling reminders
- Processing due reminders
- Converting local time to UTC
- Returning reminder information
- Marking reminders as fired
- Delegating notification creation

---

# Dependencies

ReminderService depends on

```text
IReminderRepository

INotificationService

ICurrentUserService

IDateTimeProvider

ILogger<ReminderService>
```

ReminderService must NOT depend on

```text
DashboardService

TaskRepository

HabitRepository

FinanceService

XpService
```

---

# Public API

## Create Reminder

```csharp
Task<Guid> CreateReminderAsync(CreateReminderDto dto);
```

---

## Update Reminder

```csharp
Task UpdateReminderAsync(UpdateReminderDto dto);
```

---

## Cancel Reminder

```csharp
Task CancelReminderAsync(Guid reminderId);
```

---

## Get Reminder

```csharp
Task<ReminderDetailsDto?> GetReminderAsync(Guid reminderId);
```

---

## Get Upcoming Reminders

```csharp
Task<IEnumerable<ReminderSummaryDto>> GetUpcomingRemindersAsync();
```

---

## Process Due Reminders

```csharp
Task ProcessDueRemindersAsync();
```

Executed by Hangfire.

---

## Mark Reminder Fired

```csharp
Task MarkReminderAsFiredAsync(Guid reminderId);
```

Internal service method.

---

# DTO Contracts

Input

```text
CreateReminderDto

UpdateReminderDto
```

Output

```text
ReminderSummaryDto

ReminderDetailsDto

ReminderDashboardDto
```

---

# Business Rules

ReminderService owns

- Reminder scheduling
- Reminder validation
- Reminder lifecycle
- Time zone conversion

---

## Creation

A reminder

- belongs to one User;
- may reference one Task;
- may reference one Habit;
- stores UTC internally.

---

## Processing

When a reminder becomes due, `IReminderProcessingService` supplies an explicit
UserId to the repository. `IReminderRepository` performs validation, notification
insert, and the Pending-to-Fired transition in one aggregate transaction. The
interactive ReminderService is current-user scoped and does not call
NotificationService to fire a reminder.

A reminder can only fire once.

---

## Time Zone Rules

Users enter reminder time in local time.

ReminderService converts

```
Local Time

↓

UTC

↓

Database
```

Display

```
Database UTC

↓

User Local Time
```

---

## Idempotency

Running reminder processing multiple times must never create duplicate notifications.

---

# Validation Rules

ReminderService validates

- ownership;
- reminder date;
- reminder status;
- duplicate firing;
- time zone.

---

# Exception Contracts

```text
ReminderNotFoundException

ReminderAlreadyFiredException

ReminderOwnershipException

ReminderValidationException
```

---

# Notification Integration

Reminder firing is not delegated to `INotificationService`; the repository owns
the atomic aggregate operation. `Triggered` is not a persisted status.

---

# Repository Usage

Allowed

```text
Add

Update

Delete

GetPending

GetUpcoming

GetDue

MarkFired
```

Repositories never schedule reminders.

---

# Return Contracts

Mutations

```
Task

Guid
```

Queries

```
DTOs
```

---

# Logging

Log

- Reminder Created
- Reminder Updated
- Reminder Cancelled
- Reminder Fired
- Reminder Failure
- Unexpected Failure

---

# Forbidden Responsibilities

ReminderService must never

- award XP;
- calculate dashboard;
- calculate finance;
- access UI.

---

# Future Expansion

Future versions may add

- Recurring reminders
- Snooze
- Browser Push
- Email
- Mobile Push
- Reminder History
- Reminder Groups

---

# 16. NotificationService

## Purpose

NotificationService owns every notification inside LifeOS.

Notifications represent user-visible events.

---

# Responsibilities

NotificationService is responsible for

- Creating notifications
- Retrieving notifications
- Marking notifications read
- Dismissing notifications
- Returning unread counts

---

# Dependencies

```text
INotificationRepository

ICurrentUserService

ILogger<NotificationService>
```

---

# Public API

```csharp
Task<Guid> CreateNotificationAsync(CreateNotificationDto dto);

Task<IEnumerable<NotificationDto>> GetNotificationsAsync();

Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync();

Task<int> GetUnreadCountAsync();

Task MarkAsReadAsync(Guid notificationId);

Task DismissAsync(Guid notificationId);

// No public notification delete or undismiss operation exists in Milestone 6.

No public notification delete or undismiss operation exists in Milestone 6.
```

---

# Business Rules

Notifications

- belong to one User;
- are immutable once created;
- may only change Read/Dismiss state.

---

# Validation Rules

Validate

- ownership;
- notification state.

---

# Exception Contracts

```text
NotificationNotFoundException

NotificationOwnershipException
```

---

# Repository Usage

Allowed

```text
Add

GetUnread

GetAll

Update

Delete
```

---

# Forbidden Responsibilities

NotificationService must never

- schedule reminders;
- award XP;
- calculate dashboard.

---

# Future Expansion

Future versions

- Notification Categories
- Notification Preferences
- Push Channels
- Email Channels
- AI Insight Notifications

---

# Milestone 6 contract addendum

`IReminderService` and `INotificationService` are current-user scoped. Background
due discovery uses `IReminderProcessingService`, explicit candidate UserIds, and
never `ICurrentUserService`. Reminder firing is an `IReminderRepository`
aggregate operation: it rechecks user, status, version, and due state, inserts
one notification, sets Fired/FiredAtUtc/NotificationId, and commits atomically.
Each candidate has its own transaction; failures leave Pending, later candidates
are attempted, and the batch reports an aggregate failure for Hangfire retry.

Interactive queries are bounded: 100 pending reminders ordered by
`ScheduledForUtc`, 100 newest non-dismissed notifications, and three pending
Dashboard reminders. Reminder notifications link to the reminder and safely
resolve a Task/Habit source only when available for the current user;
progression notifications link to `/` or have no source link. No generic deep-link
or polymorphic source infrastructure is introduced.

# 17. FinanceService

## Purpose

FinanceService owns all financial business logic inside LifeOS.

It manages manual transactions, monthly summaries, category breakdowns, and financial calculations.

FinanceService is the only service allowed to calculate financial summaries.

---

# Responsibilities

FinanceService is responsible for

- Creating transactions
- Updating transactions
- Archiving transactions
- Monthly summaries
- Category summaries
- Remaining balance
- Monthly plan
- Default finance categories

---

# Dependencies

```text
IFinanceRepository

ICurrentUserService

IDateTimeProvider

ILogger<FinanceService>
```

FinanceService must NOT depend on

```text
TaskService

HabitService

XPService

DashboardService
```

---

# Public API

## Create Transaction

```csharp
Task<Guid> CreateTransactionAsync(CreateFinanceTransactionDto dto);
```

---

## Update Transaction

```csharp
Task UpdateTransactionAsync(UpdateFinanceTransactionDto dto);
```

---

## Archive Transaction

```csharp
Task ArchiveTransactionAsync(Guid transactionId);
```

---

## Delete Transaction

```csharp
Task DeleteTransactionAsync(Guid transactionId);
```

---

## Get Transaction

```csharp
Task<FinanceTransactionDto?> GetTransactionAsync(Guid transactionId);
```

---

## Get Monthly Summary

```csharp
Task<FinanceSummaryDto> GetMonthlySummaryAsync(
    int year,
    int month);
```

---

## Get Category Breakdown

```csharp
Task<IEnumerable<CategorySummaryDto>>
GetCategoryBreakdownAsync(
    int year,
    int month);
```

---

## Get Monthly Plan

```csharp
Task<MonthlyFinancePlanDto>
GetMonthlyPlanAsync(
    int year,
    int month);
```

---

## Save Monthly Plan

```csharp
Task SaveMonthlyPlanAsync(
    MonthlyFinancePlanDto dto);
```

---

# DTO Contracts

Input

```text
CreateFinanceTransactionDto

UpdateFinanceTransactionDto

MonthlyFinancePlanDto
```

Output

```text
FinanceTransactionDto

FinanceSummaryDto

CategorySummaryDto

FinanceDashboardDto
```

---

# Business Rules

FinanceService owns

- Remaining Balance
- Monthly Totals
- Category Totals
- Monthly Allowance
- Monthly Plan

---

## Remaining Balance

Formula

```
Expected Income

+

Income Transactions

-

Expense Transactions

=

Remaining Balance
```

---

## Monthly Summary

Uses

```
TransactionDate
```

Never

```
CreatedAt
```

---

## Transactions

Amount

Must always be positive.

Transaction Type determines

Income

or

Expense.

---

## Categories

Categories are

- user scoped;
- configurable;
- reusable.

---

# Validation Rules

Validate

- amount;
- category;
- transaction type;
- ownership;
- transaction date.

---

# Exception Contracts

```text
FinanceTransactionNotFoundException

FinanceValidationException

FinanceOwnershipException

InvalidCategoryException
```

---

# Repository Usage

Allowed

```text
Add

Update

Delete

GetByMonth

GetByCategory

GetTransaction

GetMonthlyPlan
```

Repositories never calculate totals.

---

# Return Contracts

Mutations

```
Task

Guid
```

Queries

```
DTOs
```

---

# Logging

Log

- Transaction Created
- Transaction Updated
- Transaction Deleted
- Monthly Plan Updated
- Validation Failure
- Unexpected Failure

Never log

- descriptions;
- notes;
- sensitive financial information.

---

# Forbidden Responsibilities

FinanceService must never

- award XP;
- query Tasks;
- query Habits;
- calculate Dashboard;
- access UI.

---

# Future Expansion

Future versions may extend FinanceService with

- Revolut Import
- Raiffeisen Import
- Import Preview
- Duplicate Detection
- Merchant Normalisation
- Budgets
- Subscription Manager
- Savings Goals
- Net Worth
- AI Finance Reports

# Future Services

The following services are part of the long-term LifeOS architecture.

They are intentionally specified at a high level.

Detailed contracts will be created when their implementation begins.

---

# SleepService

## Purpose

Owns sleep tracking, sleep analytics, and sleep history.

## Responsibilities

- Create sleep entries
- Update sleep entries
- CancelPending
- Retrieve sleep history
- Calculate sleep statistics

## Planned Public API

```csharp
CreateSleepEntryAsync()

UpdateSleepEntryAsync()

DeleteSleepEntryAsync()

GetSleepHistoryAsync()

GetSleepStatisticsAsync()
```

---

# WorkoutService

## Purpose

Owns workout plans, workout sessions and progressive overload.

## Responsibilities

- Workout plans
- Workout sessions
- Progressive overload
- PR detection
- Workout statistics

## Planned Public API

```csharp
CreateWorkoutPlanAsync()

StartWorkoutAsync()

CompleteWorkoutAsync()

LogSetAsync()

GetWorkoutHistoryAsync()

GetWorkoutStatisticsAsync()
```

---

# NutritionService

## Planned Public API

```csharp
CreateMealAsync()

UpdateMealAsync()

DeleteMealAsync()

GetDailyNutritionAsync()

GetNutritionStatisticsAsync()
```

---

# BodyMetricsService

## Planned Public API

```csharp
LogBodyMetricsAsync()

UpdateBodyMetricsAsync()

GetWeightHistoryAsync()

GetMeasurementHistoryAsync()
```

---

# StudyService

## Planned Public API

```csharp
CreateSubjectAsync()

LogStudySessionAsync()

GetStudyStatisticsAsync()

GetWeeklyProgressAsync()
```

---

# ProjectService

## Planned Public API

```csharp
CreateProjectAsync()

UpdateProjectAsync()

LogProjectSessionAsync()

GetProjectStatisticsAsync()
```

---

# FocusSessionService

## Planned Public API

```csharp
StartFocusSessionAsync()

CompleteFocusSessionAsync()

CancelFocusSessionAsync()

GetFocusStatisticsAsync()
```

---

# WellbeingService

## Planned Public API

```csharp
CreateDailyCheckInAsync()

UpdateDailyCheckInAsync()

GetWellbeingHistoryAsync()
```

---

# JournalService

## Planned Public API

```csharp
CreateJournalEntryAsync()

UpdateJournalEntryAsync()

DeleteJournalEntryAsync()

SearchJournalAsync()
```

---

# AIService

## Purpose

Acts as the application entry point for all AI interactions.

AIService never queries the database directly.

It consumes other Application Services.

## Planned Public API

```csharp
ChatAsync()

GenerateWeeklyReviewAsync()

GenerateInsightAsync()

GenerateFinanceSummaryAsync()

GenerateWorkoutSummaryAsync()
```

---

# ImportService

## Planned Public API

```csharp
PreviewImportAsync()

ValidateImportAsync()

ConfirmImportAsync()

CancelImportAsync()
```

---

# GarminImportService

## Planned Public API

```csharp
ImportSleepAsync()

ImportWorkoutAsync()

ImportRecoveryAsync()