# 06 - Milestone Build Plan

## 1. Purpose

This document defines the recommended build order for the LifeOS rebuild. It is designed to avoid the prototype pattern of building too much before the foundation is stable.

The plan contains:

- V1 milestones;
- definitions of done;
- future release themes that preserve all major modules.

## 2. Build philosophy

Each milestone should produce a stable increment.

A milestone is not done because UI exists. A milestone is done when:

- entities are correct;
- migrations exist;
- services are implemented;
- database constraints are present;
- business rules are tested where important;
- UI works;
- build passes;
- acceptance checklist passes;
- documentation is updated.

## 3. V1 milestones

## Milestone 0 - Documentation and repository setup

Goal: create a clean starting point.

Scope:

- finalize documentation pack;
- create repository;
- add README;
- add AGENTS.md for Copilot;
- add docs folder;
- create issue/ticket template;
- define branch and commit conventions.

Done when:

- docs are committed;
- V1 scope is explicit;
- future modules are preserved in backlog;
- Copilot workflow is documented.

## Milestone 1 - Solution foundation

Goal: scaffold the app without feature complexity.

Scope:

- create solution;
- create Web/Core/Infrastructure projects;
- add test project;
- configure project references;
- add app shell;
- add dark theme foundation;
- add basic navigation;
- configure PostgreSQL;
- configure EF Core;
- configure the development current-user implementation;

Done when:

- app runs locally;
- development user can access dashboard placeholder through `ICurrentUserService`;
- clean database migration succeeds;
- build passes.

## Milestone 2 - Architecture primitives

Goal: implement rules that all features rely on.

Scope:

- BaseEntity;
- user-owned entity pattern;
- current user service;
- date/time provider;
- user settings with time zone;
- soft delete behavior;
- audit timestamp handling;
- service registration pattern;
- error handling pattern;
- logging pattern.

Done when:

- services can access current user;
- audit timestamps are set consistently;
- soft-deleted records are excluded from normal queries;
- date/time provider is used by new services;
- build/tests pass.

## Milestone 3 - Tasks vertical slice

Goal: tasks work end to end.

Scope:

- TaskItem entity;
- enums for status, priority, category, estimated time, friction;
- EF configuration;
- migration;
- task service;
- task list page;
- add/edit task;
- complete task with no XP integration;
- delete/archive task;
- task dashboard widget through DashboardService, limited to task data;
- basic tests.

Done when:

- task CRUD works;
- user cannot access another user's tasks;
- due/today/overdue display is correct;
- soft delete works;
- completing an already-completed task is a successful no-op that preserves the original completion timestamps;
- completed and archived tasks are read-only;
- active task lists classify tasks as Overdue, Today, Upcoming, or Unscheduled;
- build/tests pass.

## Milestone 4 - Habits vertical slice

Status: Complete — Ticket 16 verification passed

Goal: habits and completion logs work reliably.

Scope:

- Habit entity;
- HabitLog entity;
- frequency and target enums, with Daily as the only supported frequency;
- unique constraint on `(UserId, HabitId, CompletionDate)`;
- HabitService implemented in Infrastructure behind Core contracts;
- create, read, update, and archive workflows;
- Habit editor and habit management/today-completion UI;
- complete today's habit using the user's local date;
- immutable binary HabitLog completion events;
- idempotent duplicate completion and PostgreSQL race-safety;
- basic current daily streak calculation;
- simple newest-first completion history for active and archived habits;
- widget-specific DashboardService Habit capability with completion progress;
- unit, model, service, repository, and PostgreSQL integration tests.

Milestone 4 HabitService dependencies are `IHabitRepository`, `ICurrentUserService`, `IUserSettingsService`, `IDateTimeProvider`, and `ILogger<HabitService>`. XP belongs to Milestone 5; reminders and notifications belong to Milestone 6.

Milestone 4 does not include restore/reactivate, user-facing Habit deletion or soft deletion, multiple daily completions, quantity achievement entry, XP preview or awarding, reminder scheduling, or broad Habit statistics.

Done when:

- habits can be created and completed;
- active habits can be edited and archived habits are read-only;
- archived habits remain available for history and cannot be completed;
- duplicate completion is a successful no-op and duplicate logs cannot be created through UI, service, or database;
- streak display follows the documented current daily streak rules;
- history is user-scoped and ordered newest-first;
- dashboard Habit widget shows active daily completion progress;
- build/tests pass.

## Milestone 5 - XP and progression core

Goal: make gamification trustworthy.

Implementation order:

1. documentation alignment and executable baseline;
2. deterministic XP/progression rules;
3. XP domain and contracts;
4. EF model;
5. migration and PostgreSQL schema;
6. atomic XP repository;
7. XP service;
8. Task completion hardening and XP integration;
9. Habit XP integration;
10. Dashboard XP projection;
11. XP widget;
12. completion feedback and Dashboard coordination;
13. final PostgreSQL, concurrency, regression, and manual closure.

Scope includes the append-only `XpTransaction` ledger, lazy one-per-user `UserProgression`, exact Quest XP and level/echelon rules, shared 500-XP cap, deterministic idempotency, duplicate protection, progression concurrency, atomic XP/progression persistence, current progression/history service queries, Task/Habit integration, transition metadata, and the XP Progress Dashboard widget.

Milestone 5 detects level/echelon changes but does not create persisted notifications. Notification persistence and user-facing level/echelon notification creation belong to Milestone 6.

Out of scope:

- DailyScore calculation or scheduled processing;
- streak bonus XP;
- Notification entity, persistence, service, or UI;
- global/header XP display, XP history UI, analytics, charts, achievements, badges, unlocks, or privileges;
- backfill, reversal, compensation, outbox, reconciliation jobs, background jobs, or generic repository/Unit of Work architecture.

Done when:

- XP transactions are append-only;
- duplicate completion does not duplicate XP;
- daily cap works;
- user progression updates atomically;
- level/echelon calculations are tested;
- the frozen contracts in this plan cannot be silently changed by later implementation tickets;
- the complete migration chain, cross-source cap behavior, Task/Habit regressions, Dashboard XP projection, and manual UI refresh behavior are verified;
- build/tests pass.

## Milestone 6 - Notifications and one-time reminders

Goal: build dependable in-app reminders.

Scope:

- Reminder entity;
- Notification entity;
- notification service;
- reminder service;
- one-time reminder create/edit/cancel;
- local-time to UTC conversion;
- background due reminder job;
- notification bell/list;
- mark read/dismiss;
- idempotent reminder firing;
- tests.

### Frozen Milestone 6 contract

Milestone 6 is limited to **Notifications and One-Time Reminders**. This section is
the normative contract for all product, engineering, operations, and testing
documents. Historical descriptions elsewhere are non-authoritative when they
conflict with this section.

#### Scope and lifecycle

M6 requires persisted user-owned `Reminder` and `Notification` records; one-time
creation, pending edit, pending cancellation, Task/Habit/custom entry points,
local scheduling, due processing, in-app listing, unread count, mark-read, dismiss,
the header bell, Reminders and Notifications pages, a read-only Dashboard
Reminder widget, and level/echelon notifications. Recurring reminders, recurrence,
snooze, delivery history, external channels, templates, Quick Add infrastructure,
source-driven cancellation, generic related-object infrastructure, notification
toast/sound, mark-all-read, undismiss, public deletion, Hangfire administration,
Identity, full browser E2E automation, XP history/header XP, new XP sources or
formula changes, reconciliation/outbox, and Finance/Calendar behavior are not M6.

Reminder status is exactly `Pending = 0`, `Fired = 1`, and `Cancelled = 2`.
The only transitions are `Pending -> Fired` and `Pending -> Cancelled`;
`Fired` and `Cancelled` are terminal. `Triggered` is processing prose, never a
persisted status. Fired or cancelled reminders cannot be edited, cancelled,
reopened, or publicly deleted. Task completion, Habit completion, Task soft
deletion, and Habit deactivation never cancel a reminder; the source-title
snapshot remains and unavailable source links degrade safely.

Source types are exactly `Task = 0`, `Habit = 1`, and `Custom = 2`. Task and Habit
sources require a current-user-owned active source, a non-null `SourceId`, and a
source-title snapshot (Task title, or the current Habit name/title). Custom
requires a null `SourceId`. No generic source abstraction or polymorphic foreign
key is introduced.

#### Time and scheduling

`UserSettings` gains `DateTimeOffset? TimeZoneConfiguredAtUtc`. Null means the user
has not explicitly confirmed a timezone and reminders are disabled. Existing users
migrate with null; saving any valid timezone, including `UTC`, sets the timestamp.
The existing default remains `UTC`; browser, server, IP, and geographic timezone
inference is forbidden. Existing Task/Habit BusinessDate behavior is not gated.

Reminder input has one-minute precision and seconds are zero. The authoritative
clock is `IDateTimeProvider.UtcNow`; `ScheduledForUtc` must be strictly later than
it at save time, and due means `ScheduledForUtc <= UtcNow`. Invalid DST gap times
and ambiguous DST overlap times are rejected with a clear request to choose
another time; no offset is selected silently.

Each reminder persists its original local date, local time, timezone ID snapshot,
and resolved UTC instant. A later settings timezone change never moves an existing
reminder. Editing a pending reminder displays the stored intent and timezone and
reconverts/replaces the snapshot only when explicitly saved.

#### Persistence contract

`Reminder : UserOwnedEntity` has required `SourceType`, `Title` (max 200),
`ScheduledLocalDate`, `ScheduledLocalTime` (minute precision), `TimeZoneId` (max
100), `ScheduledForUtc`, `Status` (default Pending), `IdempotencyKey` (max 200,
`ReminderFired:{ReminderId:N}` for firing), and `Version` (long, default 0,
optimistic concurrency). `SourceId` is nullable but required for Task/Habit;
`SourceTitle` is nullable but required for Task/Habit (max 200); `Message` is
optional (max 2000); `FiredAtUtc` and `NotificationId` are nullable and required
only when Fired. Inherited `Id`, `UserId`, lifecycle/audit, and soft-delete fields
remain unchanged.

Reminder invariants require source ID/title for Task/Habit and null source ID for
Custom; Pending/Cancelled require null fired timestamp and notification ID; Fired
requires both; and `Version >= 0`. Indexes are unique `(UserId, IdempotencyKey)`,
`(UserId, Status, ScheduledForUtc)`, `(Status, ScheduledForUtc)` for workers, and
a unique filtered non-null `NotificationId`. The optional
`Reminder.NotificationId -> Notifications.Id` FK uses Restrict delete behavior.

`Notification : UserOwnedEntity` has required `Type`, `Title` (max 200), `Message`
(max 2000), optional paired `SourceType`/`SourceId`, nullable `ReadAtUtc`, nullable
`DismissedAtUtc`, and required `IdempotencyKey` (max 200). Notification types are
`ReminderDue = 0`, `LevelUp = 1`, and `EchelonChanged = 2`; source types are
`Reminder = 0` and `XpTransaction = 1`. Source fields are both null or both
non-null, and dismissal requires read. There is no persisted `IsRead`. Indexes
are unique `(UserId, IdempotencyKey)`, `(UserId, DismissedAtUtc, CreatedAtUtc)`,
and `(UserId, DismissedAtUtc, ReadAtUtc)`. There is no polymorphic FK.

#### Processing and ownership

Hangfire with PostgreSQL storage is the M6 implementation, using private schema
`hangfire`, one recurring due-reminder job every minute, at most 100 candidates per
invocation, three automatic retries, and Hangfire distributed execution
protection. The Dashboard is not exposed. Database idempotency is the final
correctness boundary. Interactive `IReminderService` and `INotificationService`
are current-user scoped. `IReminderProcessingService` is separate; worker
candidates carry explicit `UserId` and worker code never uses
`ICurrentUserService`.

Each candidate is processed in its own aggregate transaction. It rechecks
identity, explicit user, status, version, and due state, inserts exactly one
notification, changes Pending to Fired, sets `FiredAtUtc` and `NotificationId`,
and commits together. The operation belongs to `IReminderRepository`, not a
ReminderService-to-NotificationService sequence and not a Unit of Work. A failed
item remains Pending; later candidates are attempted, and an aggregate batch
failure is thrown after the batch so Hangfire retries. Successful items remain
committed.

#### XP, UI, and verification

A committed XP award that increases level creates one `LevelUp` notification; an
echelon change creates one `EchelonChanged` notification; crossing both creates
two. Ordinary XP, duplicate requests, no-transaction requests, cap-zero outcomes,
and historical Milestone 5 transactions create none. XP ledger transaction,
progression mutation, and notifications share the existing XP aggregate
transaction; the stable XP transaction ID supplies notification idempotency. The
existing completion-before-XP partial-success boundary and XP formulas/caps remain
unchanged; TD-006 stays open.

The Dashboard keeps widget-specific architecture. Its independently loaded
Reminder widget shows at most the next three pending reminders ordered by due
instant, with loading/empty/error/retry states and `Open reminders`. Notification
presence is only the header bell. The Reminders page reads the next 100 pending
items; the Notifications page reads the newest 100 non-dismissed items. The bell
polls unread count every 30 seconds while active and displays counts above 99 as
`99+`. Task/Habit DTOs do not gain reminder fields and Task/Habit persistence is
not transactional with reminder creation. M6 includes targeted bUnit coverage and
mandatory manual browser verification; full browser E2E automation is future
work. This partially mitigates TD-007, which remains open.

Done when:

- reminder set for a local time fires at that intended local time;
- duplicate job execution does not create duplicate notifications;
- notifications are user-scoped;
- read/dismiss works;
- build/tests pass.

## Milestone 7 - Simple finance

Goal: add practical monthly manual finance tracking.

Scope:

- FinanceCategory entity;
- FinanceTransaction entity;
- MonthlyFinancePlan entity;
- default categories;
- finance service;
- add/edit/delete manual transaction;
- monthly plan with planned income/allowance;
- monthly totals;
- category summary;
- remaining planned balance formula;
- dashboard finance card;
- settings for allowance/currency;
- tests.

Done when:

- user can enter income and expenses;
- monthly totals are correct;
- category totals are correct;
- remaining allowance/balance is correct;
- allowance/planned income is not double-counted;
- no import features are added;
- build/tests pass.

## Milestone 8 - V1 polish and release hardening

Goal: make V1 pleasant and safe enough to use daily.

Scope:

- responsive layout pass;
- empty states;
- loading states;
- validation messages;
- error boundaries;
- PWA manifest verification;
- navigation cleanup;
- dashboard polish;
- final database review;
- test pass;
- documentation update;
- backlog triage.

Done when:

- V1 acceptance checklist passes;
- clean database setup works;
- app can be used for a week without data integrity issues;
- docs reflect actual behavior.

## 4. Future milestones

## V1.1 - Lightweight life logs

Candidate scope:

- daily wellbeing check-in;
- basic sleep logging;
- simple journal;
- weekly intentions;
- dashboard widgets for these modules;
- first DailyScore implementation using configured modules only.

Purpose:

- capture high-signal subjective and recovery data;
- prepare for AI weekly review.

## V2 - Fitness and progressive overload

Candidate scope:

- exercise library;
- workout plan builder;
- workout days;
- planned exercises;
- session logger;
- set logging;
- rest timer;
- PR detection;
- stall detection;
- lift charts;
- workout dashboard widget.

Purpose:

- support gym programming and aesthetic physique goals.

## V2.5 - Body metrics and nutrition

Candidate scope:

- body weight and measurement logs;
- phase tagging;
- progress photo support;
- nutrition targets;
- meal logs;
- meal templates;
- meal prep planner;
- physique dashboard.

Purpose:

- connect training, food, and physique progress.

## V3 - Study, projects, and focus

Candidate scope:

- study subjects;
- weekly targets;
- study sessions;
- projects;
- project work sessions;
- Pomodoro timer;
- neglected subject/project alerts;
- portfolio progress reports.

Purpose:

- support academic consistency and internship readiness.

## V4 - AI assistant and insight engine

Candidate scope:

- AI chat;
- approved tool functions;
- weekly review generation;
- confidence-aware insights;
- finance summaries;
- physique reports;
- study summaries;
- cross-domain correlations;
- insight inbox.

Purpose:

- turn clean historical data into recommendations and explanations.

## V5 - Advanced finance and integrations

Candidate scope:

- Revolut CSV import;
- Raiffeisen CSV/XLS import;
- import preview;
- duplicate detection;
- budgets;
- subscriptions;
- savings goals;
- net worth snapshots;
- Garmin imports;
- export/reporting.

Purpose:

- add automation after manual systems are trusted.

## 5. Codex ticket breakdown pattern

Every ticket should be small.

Example ticket size:

- good: add TaskItem entity and EF configuration;
- good: implement task service create/edit methods;
- good: add task list page using service;
- bad: build all tasks, habits, XP, and dashboard.

Ticket template:

```text
TASK:

READ FIRST:
- AGENTS.md
- relevant docs

GOAL:

SCOPE:

DO NOT:

ACCEPTANCE CRITERIA:

VERIFY:
- dotnet build
- dotnet test if tests exist

OUTPUT:
- summary
- files changed
- manual verification needed
```

## 6. Milestone exit checklist

Before closing any milestone:

- build passes;
- tests pass or missing tests are explicitly justified;
- migration applies from clean database;
- user-scoping reviewed;
- date/time behavior reviewed;
- database constraints reviewed;
- UI manually tested;
- docs updated;
- backlog updated;
- commit/tag created.
