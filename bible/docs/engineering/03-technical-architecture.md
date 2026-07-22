# 03 - Technical Architecture Document

## 1. Purpose

This document defines how LifeOS should be built. It converts the product direction and prototype lessons into concrete engineering rules.

The architecture must support a small V1 while preserving a clean path toward future modules: sleep, health, fitness, body metrics, nutrition, study, projects, wellbeing, AI, Garmin, and advanced finance.

## 2. Recommended stack

V1 stack:

- UI: Blazor Server
- Runtime: .NET
- Language: C#
- ORM: Entity Framework Core
- Database: PostgreSQL
- Authentication: ASP.NET Identity
- Background jobs: Hangfire or equivalent
- Styling: app-owned CSS with dark JARVIS-inspired design system
- Tests: .NET test project with unit and integration tests where practical

Future stack elements:

- AI orchestration: Semantic Kernel or equivalent abstraction
- Local model runtime: Ollama or equivalent local model runner
- Charts: lightweight Blazor charting or JavaScript interop
- Imports: dedicated parser services per source
- Garmin: import adapters, not direct dependency in core domain

## 3. Solution structure

Recommended structure:

```text
lifeos/
  docs/
  src/
    LifeOS.Web/
    LifeOS.Core/
    LifeOS.Infrastructure/
    LifeOS.Application/        optional future extraction only
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
- future import parsers;
- future AI connectors;
- external integration adapters.

### 3.4 LifeOS.Application optional future layer

V1 should start with a simple three-project structure: Web, Core, and Infrastructure. Add `LifeOS.Application` later only if the service layer becomes large enough to justify a separate use-case layer.

If added later, `LifeOS.Application` may contain:

- feature service interfaces;
- commands/use cases;
- DTOs/read models;
- validation rules;
- orchestration between domain and infrastructure.

Until then, service interfaces should live in Core and EF-backed implementations should live in Infrastructure. Business workflows still must not live in Web.

## 4. Dependency direction

Allowed dependency direction:

```text
V1 default:
LifeOS.Web -> LifeOS.Core
LifeOS.Web -> LifeOS.Infrastructure only for composition/DI setup
LifeOS.Infrastructure -> LifeOS.Core

Future if Application is introduced:
LifeOS.Web -> LifeOS.Application -> LifeOS.Core
LifeOS.Infrastructure -> LifeOS.Application and LifeOS.Core
```

Preferred runtime pattern:

- Web calls service interfaces.
- Core defines entities, enums, value objects, constants, and service contracts.
- Infrastructure provides EF Core DbContext, configurations, migrations, and service implementations.
- Web may reference Infrastructure only to register implementations in the composition root.

## 5. Identity and ID strategy

V1 shall use Guid IDs consistently.

- ApplicationUser should inherit from IdentityUser<Guid>.
- BaseEntity.Id should be Guid.
- UserOwnedEntity.UserId should be Guid.
- Avoid mixing string user IDs with Guid entity IDs unless the decision is made deliberately before scaffolding.
- The seeded development user must be created idempotently.
- Authentication and authorization should be present before feature pages are added.

## 6. Cross-cutting services

### 6.1 Current user service

Provide an `ICurrentUserService` that exposes:

- current user ID;
- authentication status;
- maybe display name/email;
- time zone setting if useful.

All user-owned queries must use the current user service or an explicit user ID validated at the service boundary.

### 6.2 Date/time provider

Provide an `IDateTimeProvider` or `IClock` that exposes:

- `UtcNow`;
- current local date for a user time zone;
- conversion helpers if appropriate.

Do not scatter `DateTime.Now` or `DateTime.UtcNow` across services.

### 6.3 Time zone service

Because reminders rely on correct local-time behavior, provide a time zone policy:

- store user's time zone ID in settings as an IANA time zone ID, for example Europe/Bucharest;
- convert local form values to UTC before persistence;
- display UTC instants in local time;
- use `DateOnly` for business dates that do not represent instants.

### 6.4 Notification service

Provide an `INotificationService` for:

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
- detecting level/echelon changes;
- creating notifications for progression events.

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
- monthly totals;
- category summaries;
- monthly plan and remaining-balance calculations.

Future import services should not bypass finance service rules.

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
- future sleep, health, fitness, body, nutrition, study, project, wellbeing, AI, and import records.

### 7.3 Soft delete

Soft delete should be used for records where history matters.

Use soft delete for:

- tasks;
- habits;
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
- sleep windows: store bedtime/wake time as instants if time zone is known;
- Garmin imports: store source metadata and imported time zone/offset where available.

Avoid global timestamp behavior switches as a permanent solution.

## 9. Background jobs

V1 background jobs:

- reminder due check job;
- optional daily cleanup or status update job;
- no DailyScore job in V1.

Future jobs:

- daily score calculation;
- streak bonus XP;
- weekly review generation;
- AI insight generation;
- import processing;
- Garmin import processing;
- budget alert generation;
- training stall detection;
- neglected subject detection.

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

Future pages:

```text
Pages/
  SleepHealth/
  Fitness/
  BodyMetrics/
  Nutrition/
  Study/
  Projects/
  Wellbeing/
  AI/
```

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

V1 finance is manual-only.

V1 finance services:

- transaction CRUD;
- category handling;
- monthly finance plan;
- monthly summary;
- remaining balance calculation using planned income/allowance plus manual income transactions minus expenses;
- no import pipeline.

Future finance import architecture:

- source-specific parser service;
- import batch entity;
- preview step;
- duplicate detection;
- normalization service;
- transaction confirmation step;
- AI categorization only after deterministic import is reliable.

## 13. AI architecture

AI is future scope but should be architecturally anticipated.

Rules for future AI:

- AI does not query the database directly.
- AI receives data through approved service functions.
- AI responses should cite or describe data basis.
- AI should include confidence when sample size is low.
- AI should not create, update, or delete data without explicit user confirmation.
- AI prompts should be scoped to current user and relevant time range.

Future AI modules may include:

- chat service;
- insight generator;
- weekly review service;
- module-specific report generators;
- prompt templates;
- model provider abstraction;
- local model connector;
- hosted model connector if ever needed.

## 14. Future module extensibility

### 14.1 Sleep and health

Design with manual and imported data sources in mind. Use nullable fields where imported devices provide more data than manual logs.

### 14.2 Fitness

Exercise and session logging requires careful relational design. Avoid stuffing set data into JSON until the reporting needs are clear.

### 14.3 Body metrics

Photos should be stored through a file storage abstraction, not directly tied to UI paths.

### 14.4 Nutrition

Keep practical macro tracking. Avoid committing to a full food database unless the product direction changes.

### 14.5 Study/projects

Study sessions and project work sessions can likely share a generalized focus/work session model later, but avoid premature abstraction in V1.

### 14.6 Wellbeing

Wellbeing data is sensitive. Treat journal content as high privacy and do not send it into AI context unless the user opts in.

### 14.7 Garmin

Use import adapters and source metadata. Imported records should not overwrite manual records silently.

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
- create daily habit;
- update habit;
- archive or soft-delete habit;
- log completion for a user-local date idempotently;
- calculate basic current streak;
- return dashboard habit summary.

### 15.3 XP service

Required capabilities:

- calculate quest XP from estimated time and friction;
- apply daily quest XP cap using user-local business date;
- create XP transaction with idempotency key;
- update UserProgression in the same transaction;
- detect level/echelon changes;
- create progression notification through notification service.

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
- set monthly finance plan;
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
