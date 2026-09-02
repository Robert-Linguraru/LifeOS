# 03 - Technical Architecture Document

## 1. Purpose

This document defines how LifeOS should be built. It converts the product direction and prototype lessons into concrete engineering rules.

The architecture must support the completed M0-M7 baseline and the approved M8-M14 roadmap. M11 Health owns body weight, sleep, and structured daily wellbeing; broader body metrics, journals, AI, integrations, study/projects, and advanced Finance remain future work.

## 2. Recommended stack

V1 stack:

- UI: Blazor Server
- Runtime: .NET
- Language: C#
- ORM: Entity Framework Core
- Database: PostgreSQL
- Current user: `ICurrentUserService` with `DevelopmentCurrentUserService`; ASP.NET Identity is future integration work
- Background jobs: Hangfire or equivalent
- Styling: app-owned CSS with dark JARVIS-inspired design system
- Tests: .NET test project with unit and integration tests where practical

Future stack elements must be selected only when a separately approved milestone requires them. M8–M12 must not introduce AI orchestration, external-import adapters, device provenance, or generic analytics infrastructure.

## 3. Solution structure

Recommended structure:

```text
lifeos/
  docs/
  src/
    LifeOS.Web/
    LifeOS.Core/
    LifeOS.Infrastructure/
  tests/
    LifeOS.Tests/
  AGENTS.md
  README.md
```

### 3.1 LifeOS.Web

Responsibilities:

- Blazor pages and components;
- layout and navigation;
- forms and validation display;
- authentication UI;
- dashboard composition;
- calls to application services;
- no domain-heavy business logic.

Rules:

- Razor pages shall not award XP directly.
- Razor pages shall not schedule reminders directly.
- Razor pages shall not calculate streaks directly.
- Razor pages shall not own finance aggregation logic.
- Razor pages may use read models returned by services.

### 3.2 LifeOS.Core

Responsibilities:

- domain entities;
- enums;
- value objects;
- domain interfaces;
- business constants;
- domain-level rules where no infrastructure is needed.

Core should not depend on Web or Infrastructure.

### 3.3 LifeOS.Infrastructure

Responsibilities:

- EF Core DbContext;
- entity configurations;
- migrations;
- repositories if used;
- Identity persistence;
- background job implementations;
- notification persistence;
- future infrastructure only when separately approved.

### 3.4 Service placement

LifeOS uses three projects: Web, Core, and Infrastructure. There is no separate Application project.

- Core contains service contracts, DTOs/read models, entities, enums, and application exceptions.
- Infrastructure contains EF-backed repositories and service implementations, EF configuration, and persistence concerns.
- Web contains Razor/UI and the composition root.

Business workflows still must not live in Web. A separate Application project is not planned; reconsidering that structure requires a future architecture decision.

## 4. Dependency direction

Allowed dependency direction:

```text
LifeOS.Web -> LifeOS.Core
LifeOS.Web -> LifeOS.Infrastructure only for composition/DI setup
LifeOS.Infrastructure -> LifeOS.Core
```

Preferred runtime pattern:

- Web calls service interfaces.
- Core defines entities, enums, value objects, constants, and service contracts.
- Infrastructure provides EF Core DbContext, configurations, migrations, and service implementations.
- Web may reference Infrastructure only to register implementations in the composition root.

## 5. Identity and ID strategy

V1 shall use Guid IDs consistently.

- Identity integration is future work. When introduced, its user type should use Guid keys and must not absorb user-preference fields.
- BaseEntity.Id should be Guid.
- UserOwnedEntity.UserId should be Guid.
- Avoid mixing string user IDs with Guid entity IDs unless the decision is made deliberately before scaffolding.
- The development user configuration must be idempotent.
- Until Identity is introduced, `DevelopmentCurrentUserService` is the valid implementation of `ICurrentUserService`.

## 6. Cross-cutting services

### 6.1 Current user service

`ICurrentUserService` is the application-wide current-user abstraction. Its current contract exposes:

- current user ID;
- authentication status.

All user-owned queries must use this service or an explicit user ID validated at the service boundary. Identity, `HttpContext`, claims, and an Identity user type must remain behind a future implementation of this abstraction.

### 6.2 Date/time provider

Provide an `IDateTimeProvider` or `IClock` that exposes:

- `UtcNow`;
- current local date for a user time zone;
- conversion helpers if appropriate.

Do not scatter `DateTime.Now` or `DateTime.UtcNow` across services.

### 6.3 Time zone service

User time zone is stored on `UserSettings`. Services that require user preferences must use `IUserSettingsService` or the appropriate UserSettings abstraction. The time-zone policy is:

- store user's time zone ID in settings as an IANA time zone ID, for example Europe/Bucharest;
- convert local form values to UTC before persistence;
- display UTC instants in local time;
- use `DateOnly` for business dates that do not represent instants.

### 6.4 Future cross-cutting services

The following services belong to later milestones and must not be introduced as dependencies of the Milestone 3 Task slice:

- creating notifications;
- listing unread notifications;
- marking notifications read/dismissed;
- future notification channel routing.

### 6.5 XP service

Provide an `IXpService` for:

- calculating quest XP;
- enforcing daily caps;
- creating XP transactions;
- updating user progression;
- detecting level/echelon changes and returning transition metadata;
- current progression and newest-first XP history queries.

Milestone 5 uses one `IXpRepository` aggregate persistence boundary for `XpTransaction` and `UserProgression`. It does not require `INotificationService`; persisted progression notifications belong to Milestone 6.

No other service or UI component should mutate XP directly.

### 6.6 Reminder service

Provide an `IReminderService` for:

- creating reminders;
- validating reminder ownership;
- converting local time to UTC;
- fetching due reminders;
- marking reminders as fired;
- creating notifications.

### 6.7 Finance service

Provide an `IFinanceService` for:

- creating manual transactions;
- editing manual transactions;
- deleting manual transactions through the normal soft-delete infrastructure;
- monthly and yearly totals;
- category summaries;
- monthly net cash-flow calculations.

Future personal-finance capabilities require separately approved contracts and
must not expand M7 into budgeting, imports, forecasting, or accounting.

## 7. Entity and database conventions

### 7.1 Base entity

User-owned entities should inherit or include:

- `Id`;
- `UserId` where applicable;
- `CreatedAtUtc`;
- `UpdatedAtUtc`;
- `IsDeleted`;
- `DeletedAtUtc`.

Some join or configuration entities may use different keys, but audit behavior should be intentional.

### 7.2 User ownership

All personal records must have `UserId`, including:

- tasks;
- habits;
- habit logs;
- reminders;
- notifications;
- XP transactions;
- user progression;
- finance transactions;
- planned Health, Fitness, Nutrition, Calendar Event, study, project, wellbeing, and future AI records.

### 7.3 Soft delete

All `BaseEntity` types support soft deletion. `AppDbContext` currently applies audit timestamps, converts EF delete operations into soft deletes, and applies a global query filter to mapped `BaseEntity` types. User-facing lifecycle operations should still distinguish archive from soft deletion; Milestone 4 Habits use archive only and do not expose independent user-facing deletion.

Use soft delete where the entity has a valid deletion lifecycle, for example:

- tasks;
- finance transactions if summaries need historical integrity;
- reminders if audit/history matters;
- future study/project records where history matters.

Do not soft delete everything blindly. Some derived records may be append-only.

### 7.4 Constraints

Database constraints are part of feature completion, not cleanup.

Required V1 constraints include:

- one `UserProgression` per user;
- one habit log per user, habit, and date;
- one XP transaction per completion event where idempotency requires it;
- one notification per reminder fire event via notification idempotency key;
- finance amount must be greater than zero;
- DailyScore uniqueness is future scope because DailyScore is not implemented in V1.

### 7.5 Indexes

V1 index priorities:

- `UserId` on all user-owned tables;
- task status, due date, and user;
- habit active status and user;
- habit log user, habit, and date;
- reminder user, fire time, fired status;
- notification user, read status, created time;
- XP transaction user and timestamp;
- finance transaction user and transaction date/category.

Future modules should follow the same pattern: index by user, date/time, status, and foreign keys.

## 8. Date and time architecture

Use these rules:

- true instant: store as UTC timestamp;
- local display: convert from UTC to user time zone;
- calendar-only business date: use date-only type;
- month grouping: use year/month or first day of month as a date-only value;
- reminder input: parse as local wall-clock time in the user's IANA time zone, then convert to UTC;
- finance transaction dates: treat as date-only unless a future import source includes a true timestamp;
- sleep windows: design M11 manual bedtime/wake-time capture around the documented user-local date and calculate duration deterministically;
- external imports, source metadata, and provider time-zone/provenance fields: deferred until an integration milestone.

Avoid global timestamp behavior switches as a permanent solution.

## 8.1 Blazor persistence

Blazor Server persistence uses `IDbContextFactory<AppDbContext>`. Repositories and Infrastructure service implementations create and dispose a context for each operation; Razor components do not receive `AppDbContext` for feature workflows. This avoids sharing a DbContext across a Blazor circuit while preserving the service and repository boundary.

## 9. Background jobs

V1 background jobs:

- reminder due check job;
- optional daily cleanup or status update job;
- no DailyScore job in V1.

Future jobs require a concrete approved feature. M8 rest timers require no background-job architecture. AI, imports, training stall detection, and generic scoring/analytics are deferred.

Rules:

- jobs must be idempotent;
- jobs must be user-aware;
- jobs must log failures;
- jobs should use services rather than directly duplicating business rules.

## 10. UI architecture

V1 pages should call services and display view models.

Recommended feature folders:

```text
LifeOS.Web/
  Components/
  Layout/
  Pages/
    Dashboard/
    Tasks/
    Habits/
    Finance/
    Notifications/
    Settings/
  Shared/
```

Planned pages are introduced only by their owning milestone:

```text
Pages/
  Fitness/
  Nutrition/
  Health/
  Calendar/
```

Body Metrics, Journal, Study, Projects, AI, and integration pages are future concepts, not planned M8-M14 page contracts.

Reusable UI components should be created for:

- card panels;
- stat cards;
- quick-add buttons;
- empty states;
- progress bars;
- XP display;
- notification item;
- date/time input wrappers.

## 11. Module architecture

Each module should follow the same pattern:

1. Entity and enum definitions.
2. EF configuration.
3. Migration.
4. Service interface.
5. Service implementation.
6. Tests for core rules.
7. Razor UI.
8. Dashboard widget if needed.
9. Documentation update.

Do not start a feature with only UI.

## 12. Finance architecture

M7 Simple Finance is manual-only and transaction-based. It is a lightweight
personal tracker, not a budgeting, accounting, forecasting, or banking system.

M7 finance services:

- transaction CRUD;
- stable global immutable default categories;
- selected-month income, expense, net, and expense-category summaries;
- selected-year income, expense, and net summaries;
- one user-level finance currency preference;
- no import, budget, allowance, or planned-income pipeline.

Future Finance may add richer personal-finance capabilities through separately
approved milestones. M7 does not define import, budget, analytics, or
accounting architecture in advance.

## 13. Analytics and AI boundary

M14 is the first Analytics milestone and follows a period of accumulated real data. Its detailed architecture is deliberately undecided. AI is later future work, with no milestone number. When approved, it must consume structured LifeOS data, deterministic domain capabilities, deterministic analytics, and retrieval/query capabilities rather than raw-history prompts or direct database access. No AI services, provider abstractions, fields, or orchestration are introduced in M8–M14 planning.

## 14. Future module extensibility

### 14.1 Health

M11 is manual, lightweight, and non-medical: body weight, sleep, and structured daily wellbeing. Device-derived data, medical metrics, and imported-data schemas are deferred.

### 14.2 Fitness

M8/M9 use domain-specific strength-session and activity records plus read projections for unified history. The shared Exercise Library supports strength and calisthenics; no duplicate Fitness timeline or device-import schema is introduced. Avoid JSON set blobs and generic measurement engines.

### 14.3 Future body metrics and journals

Broader body measurements, physique photos, phase tracking, and journal functionality are future concepts. They require an approved scope and concrete architecture before storage, service, or UI decisions are made.

### 14.4 Nutrition

M10 separates reusable Food definitions from immutable historical food-log entries. It uses practical macros, hydration, supplements, and reusable meals; external food databases and barcode integration are deferred.

### 14.5 Study/projects

Study sessions and project work sessions can likely share a generalized focus/work session model later, but avoid premature abstraction in V1.

### 14.6 Future journal privacy

M11 structured wellbeing contains no free text. A future journal is sensitive and must not enter AI context automatically.

### 14.7 Calendar

M12 owns native Calendar Events only and projects dated domain records without duplicating them into Calendar persistence. Calendar observes Fitness; it does not schedule workouts or project ordinary Finance transactions.

## 15. Service contract baseline

The exact method names can evolve, but V1 services should expose these capabilities.

### 15.1 Task service

Required capabilities:

- list today's tasks for current user;
- list overdue tasks for current user;
- create task;
- update task;
- complete task idempotently;
- archive or soft-delete task;
- return dashboard task summary.

### 15.2 Habit service

Required capabilities:

- list active daily habits for current user;
- create daily Habit;
- update active Habit;
- archive Habit by setting `IsActive = false`;
- log binary completion for a user-local date idempotently;
- calculate basic current streak;
- return newest-first history;
- return a widget-specific dashboard Habit summary.

Milestone 4 does not include restore/reactivate, user-facing Habit deletion or soft deletion, quantity achievement entry, XP integration, or reminder integration.

### 15.3 XP service

Required capabilities:

- calculate quest XP from estimated time and friction;
- apply daily quest XP cap using user-local business date;
- create XP transaction with idempotency key;
- update UserProgression in the same transaction;
- detect level/echelon changes and return transition metadata;
- initialize progression lazily and race-safely;
- return an idempotent partial-success result when source completion succeeds but XP cannot be persisted after three attempts.

### 15.4 Reminder service

Required capabilities:

- create one-time reminder from local date/time and user time zone;
- cancel pending reminder;
- list pending reminders;
- find due reminders;
- fire reminder idempotently;
- create notification through notification service.

### 15.5 Finance service

Required capabilities:

- list transactions for selected month;
- create manual income/expense transaction;
- update manual transaction;
- archive or soft-delete manual transaction;
- calculate monthly summary and category breakdown.

## 16. Configuration and secrets

- Use user secrets for local development credentials.
- Do not commit passwords or connection strings.
- Use environment variables for deploy-time secrets.
- Separate development, test, and production settings.

## 17. Logging and observability

V1 should log:

- migration startup failures;
- background job failures;
- reminder processing failures;
- XP award failures;
- import failures in future;
- AI failures in future.

Do not log sensitive journal text, full financial descriptions, or private AI prompts unless explicitly required and safe.

## 18. Development workflow with Codex

Codex should work from small tickets, not broad goals.

Every Codex ticket should include:

- task title;
- files/docs to read;
- goal;
- scope;
- do not list;
- acceptance criteria;
- commands to run;
- expected summary.

Codex should not decide scope, stack changes, or add future modules without explicit approval.

## 19. Architecture decisions retained from prototype lessons

The rebuild must avoid these prototype failure patterns:

- missing base entity;
- weak user ownership;
- protected pages not consistently protected;
- global timestamp workaround as permanent policy;
- missing unique constraints;
- direct database access from Razor pages for business workflows;
- XP mutation outside XP service;
- reminder local-time values treated as UTC;
- background jobs added before idempotency rules;
- feature milestones considered done without migrations, constraints, and tests.

## Milestone 6 architecture boundary

M6 preserves the three-project pragmatic Clean Architecture: Core owns entities,
DTOs, enums, mappings, and interfaces; Infrastructure owns EF configuration,
migrations, repositories, service implementations, and Hangfire PostgreSQL
storage; Web consumes interfaces and DTOs. There is no Application project,
generic repository, Unit of Work, outbox, event bus, message broker, or separate
worker. Reminder firing is a repository-owned aggregate transaction and worker
ownership uses explicit UserIds rather than `ICurrentUserService`.
