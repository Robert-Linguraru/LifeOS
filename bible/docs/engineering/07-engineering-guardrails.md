# 07 - Engineering Guardrails Document

## 1. Purpose

This document defines rules that prevent the rebuild from repeating prototype mistakes.

If a Codex suggestion, implementation shortcut, or feature idea violates these guardrails, pause and resolve the conflict before continuing.

## 2. Product scope guardrails

- Implement M8-M14 only in their approved sequence and according to `16-post-m7-domain-roadmap.md`.
- Do not add AI, bank import, browser push, Garmin/device integrations, advanced body metrics, journal/weekly intentions, study/projects, or focus modules without separately approved future scope.
- Do not implement Fitness, Nutrition, Health, or Calendar capabilities before their owning milestone.
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
- Feature workflows must obtain user ownership through `ICurrentUserService`; a future Identity implementation must preserve this abstraction rather than bypass it.
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
- Tests must cover local-time reminder conversion, including explicit UTC and
  invalid/ambiguous DST cases. UTC is the default, not proof of confirmation.

## 6. XP and gamification guardrails

- XP is never user-editable.
- XP is only written by `IXpService`.
- Every positive actual XP award creates one XP transaction; a zero actual award creates none.
- User progression updates must be atomic with XP transaction creation.
- Completing a task/habit twice must not award XP twice.
- Daily quest XP cap must be enforced server-side.
- Level/echelon calculations must be deterministic and tested.
- Milestone 5 returns level/echelon transition metadata and does not create persisted notifications; notification persistence belongs to Milestone 6.

## 7. Habit guardrails

- Milestone 4 Habits are Daily-only; future frequency values must not be exposed until their schedule model exists.
- Habit completion is binary and duplicate-safe; quantity targets are definition metadata only.
- Habit names are reusable; no user/name uniqueness constraint is required for Milestone 4.
- New Habits begin active. Archive sets `IsActive = false`; archived Habits are read-only and cannot be completed.
- Milestone 4 does not provide restore/reactivate, user-facing Habit deletion/soft deletion, XP, or reminders.
- Streak calculations should use the user's local date.
- Do not implement momentum streaks until daily streaks are tested.
- Do not implement weekly streaks until weekly schedules are modeled clearly.
- Do not support multiple completions per day until the data model explicitly supports it.
- Do not support selected-day schedules until the schedule model and tests are explicitly added.

## 8. Reminder guardrails

- One-time reminders must work before recurring reminders.
- In-app notifications must work before browser push.
- Reminder firing must be idempotent.
- Reminder firing inserts the Notification and transitions the Reminder to Fired
  in one repository-owned transaction; `Triggered` is not a persisted state.
- Reminder local-time conversion must be tested.
- Reminder jobs must not duplicate business rules already in services.
- Snooze is future scope until basic reminders are reliable.
- Reminder delivery history, external delivery states, and notification channels
  are outside Milestone 6 and must not be added to its schema.

## 9. Finance guardrails

- V1 finance is manual-only.
- Do not build Revolut import in V1.
- Do not build Raiffeisen import in V1.
- Do not build merchant normalization in V1.
- Do not build subscription manager in V1.
- Do not build savings projections in V1.
- Do not build net worth in V1.
- Finance summaries must group by transaction date, not created date.
- Finance calculations must use manual income and expense transactions only in M7; plans and allowances do not exist.
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

- M8 implementation must follow `16-m8-strength-training-architecture-contract.md`.
- M8 uses one global seeded Exercise Library for strength and calisthenics; do not create a separate calisthenics domain.
- Active workouts must persist outside Blazor component memory; completed sessions are immutable historical snapshots.
- Templates are reusable recipes, not multi-week programs; do not add RPE/RIR, custom exercises, device schemas, or generic metrics.
- M9 unified Fitness history is a read projection, not a duplicate persistence table. PRs derive from Working-set/activity evidence and Fitness Goals are performance outcomes, not cumulative/frequency challenges.

### 11.2 Body metrics

- M11 Health owns body weight. Measurements, physique phases, and photos remain future work.
- Do not add medical interpretation, vitals, diagnoses, readiness scores, or device sync.

### 11.3 Nutrition

- M10 Food definitions and historical Food Log Entries are distinct; reusable-food edits cannot rewrite historical intake.
- Keep Nutrition practical: no barcode/external food database, deep micronutrients, prescriptions, supplement dosage/pill tracking, or generic conversions.

### 11.4 Study/projects

- Avoid over-abstracting study and project sessions too early.
- Pomodoro timer should not block manual logging.

### 11.5 Wellbeing/journal

- Journal text is sensitive.
- Do not send journal text to AI automatically without an explicit product decision.

### 11.6 Garmin

- Garmin, Strava, wearables, health platforms, imported data, and associated provenance/synchronization/deduplication schemas are future work.

### 11.7 Calendar and Analytics

- M12 Calendar owns native Events only. It projects dated domain records without duplicating them, does not schedule workouts, and does not project ordinary Finance transactions.
- Calendar has no Dashboard widget; its Day navigation delegates to originating domains.
- M14 Analytics follows accumulated data and M13 UI/UX polish. Do not add generic analytics infrastructure before M14.
- AI is future work after Analytics and has no milestone number.

## 12. Testing guardrails

A feature is not accepted without tests for its highest-risk rules.

Minimum V1 test areas:

- user isolation;
- habit duplicate prevention;
- XP idempotency;
- daily XP cap;
- reminder local-time conversion;
- reminder idempotent firing;
- finance monthly income, expenses, and net-cash-flow formula;
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
