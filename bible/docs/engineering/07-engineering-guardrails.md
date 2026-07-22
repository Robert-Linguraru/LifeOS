# 07 - Engineering Guardrails Document

## 1. Purpose

This document defines rules that prevent the rebuild from repeating prototype mistakes.

If a Codex suggestion, implementation shortcut, or feature idea violates these guardrails, pause and resolve the conflict before continuing.

## 2. Product scope guardrails

- Do not build outside V1 without explicit approval.
- Do not add AI in V1.
- Do not add bank import in V1.
- Do not add browser push in V1.
- Do not add workout, nutrition, study, body metrics, or Garmin modules in V1.
- Do not add selected-day habits in V1.
- Do not add the DailyScore engine in V1.
- Do not delete future modules from the official roadmap just because they are not in V1.
- Do not turn V1 into the full LifeOS.

## 3. Architecture guardrails

- Razor pages must not own business workflows.
- Use services for task, habit, XP, reminder, notification, and finance logic.
- Do not inject `AppDbContext` into Razor pages for feature workflows unless explicitly approved for read-only prototype/debug pages.
- Every personal entity must have `UserId`.
- Every user-facing service query must be user-scoped.
- Protected pages must require authorization.
- Use Guid IDs consistently unless the architecture decision is changed before scaffolding.
- Do not hard-code single-user assumptions into services.
- Do not introduce packages casually. New dependencies need a reason.

## 4. Database guardrails

- Database constraints are required for business uniqueness.
- UI guards are not enough.
- Habit logs must have a unique user/habit/date constraint in V1.
- Daily score must have a unique user/date constraint when implemented later.
- User progression must be unique per user.
- XP idempotency must be enforceable.
- Important query paths need indexes before a feature is accepted.
- Migrations must run from a clean database.
- Seed operations must be idempotent.

## 5. Date and time guardrails

- Do not use a global timestamp workaround as permanent policy.
- Do not use `DateTime.Now` in business logic.
- Do not treat `datetime-local` form input as UTC.
- Store true instants in UTC.
- Store calendar dates as date-only values.
- Convert reminder local times using the user's time zone.
- Finance transaction dates are date-only unless a source provides a real timestamp.
- Tests must cover local-time reminder conversion, including UTC and Europe/Bucharest.

## 6. XP and gamification guardrails

- XP is never user-editable.
- XP is only written by `IXpService`.
- Every XP award creates an XP transaction.
- User progression updates must be atomic with XP transaction creation.
- Completing a task/habit twice must not award XP twice.
- Daily quest XP cap must be enforced server-side.
- Level/echelon calculations must be deterministic and tested.
- Level-up/echelon notifications should be emitted through notification service, not UI shortcuts.

## 7. Habit guardrails

- Habit completion must be duplicate-safe.
- Streak calculations should use the user's local date.
- Do not implement momentum streaks until daily streaks are tested.
- Do not implement weekly streaks until weekly schedules are modeled clearly.
- Do not support multiple completions per day until the data model explicitly supports it.
- Do not support selected-day schedules until the schedule model and tests are explicitly added.

## 8. Reminder guardrails

- One-time reminders must work before recurring reminders.
- In-app notifications must work before browser push.
- Reminder firing must be idempotent.
- Reminder local-time conversion must be tested.
- Reminder jobs must not duplicate business rules already in services.
- Snooze is future scope until basic reminders are reliable.
- Reminder delivery history is future scope but should be considered in schema design.

## 9. Finance guardrails

- V1 finance is manual-only.
- Do not build Revolut import in V1.
- Do not build Raiffeisen import in V1.
- Do not build merchant normalization in V1.
- Do not build subscription manager in V1.
- Do not build savings projections in V1.
- Do not build net worth in V1.
- Finance summaries must group by transaction date, not created date.
- Finance calculations must not double-count planned allowance/income and income transactions.
- Money must use decimal, not floating point.
- AI finance commentary is future scope and must not sound like professional financial advice.

## 10. AI guardrails

- AI waits until clean data exists.
- AI does not query the database directly.
- AI must be scoped to the current user.
- AI must distinguish facts from suggestions.
- AI must mention low confidence when data is sparse.
- AI must not provide medical or financial certainty.
- AI must not silently mutate data.
- Journal and wellbeing data require special privacy care.

## 11. Future module guardrails

### 11.1 Fitness

- Do not add workout features until core task/habit/XP patterns are stable.
- Session logging must model sets properly.
- Progressive overload requires reliable historical data.

### 11.2 Body metrics

- Progress photos need a storage abstraction.
- Body analysis must avoid medical certainty.
- Phase context matters for interpreting trends.

### 11.3 Nutrition

- Keep nutrition practical.
- Do not build a full food database unless explicitly decided later.
- Estimated macros are acceptable.

### 11.4 Study/projects

- Avoid over-abstracting study and project sessions too early.
- Pomodoro timer should not block manual logging.

### 11.5 Wellbeing/journal

- Journal text is sensitive.
- Do not send journal text to AI automatically without an explicit product decision.

### 11.6 Garmin

- Imported and manual records must be distinguishable.
- Imports must preview before mutating lots of data.

## 12. Testing guardrails

A feature is not accepted without tests for its highest-risk rules.

Minimum V1 test areas:

- user isolation;
- habit duplicate prevention;
- XP idempotency;
- daily XP cap;
- reminder local-time conversion;
- reminder idempotent firing;
- finance monthly totals and remaining-balance formula;
- migration from clean database.

## 13. Documentation guardrails

- When behavior changes, update docs.
- When a feature is postponed, move it to backlog instead of deleting it.
- When Codex implements a ticket, compare it against the relevant docs.
- Do not let generated code become the source of truth without updating documentation.

## 14. Codex guardrails

Codex should be instructed to:

- read AGENTS.md and relevant docs first;
- propose a short plan for complex tasks;
- keep changes scoped;
- not expand V1;
- run build/tests;
- summarize changed files;
- state anything not completed.

Do not accept broad Codex changes without reviewing the diff.

## 15. Stop conditions

Stop and reassess if:

- a ticket changes unrelated modules;
- a feature requires schema changes not in the data model doc;
- build breaks and Codex cannot explain why;
- a migration cannot apply to a clean database;
- reminders behave differently in local time and UTC;
- duplicate XP appears;
- finance totals are inconsistent;
- UI starts calling infrastructure directly for business workflows;
- future modules start creeping into V1.
