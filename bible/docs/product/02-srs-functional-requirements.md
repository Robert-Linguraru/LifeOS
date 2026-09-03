# 02 - SRS / Functional Requirements Document

## 1. Purpose

This document defines how LifeOS should behave. It separates V1 requirements from future requirements.

Requirement labels:

- `V1` means required for the first rebuilt version.
- `M8` through `M14` mean planned work in the approved milestone; detailed planned requirements are canonical in `16-post-m7-domain-roadmap.md`.
- `Future` means preserved in the official roadmap but not part of V1.

## 2. Global system requirements

### 2.1 Identity and access

- `Current` The system shall use `ICurrentUserService` to scope all personal app workflows; the development implementation supplies the configured development user.
- `Future Identity milestone` The system shall require authentication for all personal app pages without bypassing `ICurrentUserService`.
- `V1` The system shall store a `UserId` on every user-owned entity.
- `V1` The system shall ensure all service queries are scoped to the current user.
- `Future` The system shall support multiple users without requiring destructive schema redesign.

### 2.2 Audit and lifecycle behavior

- `V1` User-owned entities shall inherit shared audit fields.
- `V1` The system shall store creation and update timestamps.
- `V1` The system shall prefer soft delete for user-owned records that may be relevant to history.
- `V1` The system shall exclude soft-deleted records from normal queries.
- `V1` The system shall not physically delete records required for XP, history, or audit accuracy.

### 2.3 Date and time behavior

- `V1` The system shall store true instants in UTC.
- `V1` The system shall store calendar-only values as date-only values.
 - `V1` The system shall store user time zone as an IANA time zone ID, defaulting to `UTC`; explicit confirmation is separately recorded for reminders.
- `Milestone 6` The system shall configure the user's time zone before reminders are enabled.
- `V1` The system shall convert user-entered local reminder times to UTC using the user's configured time zone.
- `V1` The system shall not treat an unspecified local `datetime-local` input as UTC.
- `V1` The system shall use a date/time abstraction rather than hard-coding current time throughout business logic.

### 2.4 Validation and feedback

- `V1` The system shall validate required fields before saving.
- `V1` The system shall show clear error messages for invalid input.
- `V1` The system shall prevent duplicate records through both service logic and database constraints where appropriate.
- `V1` The system shall fail gracefully if a background job or database operation fails.

## 3. Task requirements

### 3.1 Task creation and editing

- `V1` The system shall allow the user to create a one-time task.
- `V1` A task shall have a title.
- `V1` A task may have a description or notes.
- `V1` A task may have a due date.
- `V1` A task may have a due time.
- `V1` A due time shall require a due date.
- `V1` Task due dates and due times shall be treated as planning fields in the user's local time zone.
- `Milestone 6` Reminder delivery for a task shall be represented by a Reminder record, not by ad hoc task due-time logic.
- `V1` A task may have a priority.
- `V1` A task may have a category or domain.
- `V1` A task may have estimated time.
- `V1` A task may have friction level.
- `V1` The system shall allow editing active task fields.
- `V1` Completed and archived tasks shall be read-only.

### 3.2 Task completion

- `V1` The system shall allow the user to mark a task complete.
- `V1` Completing a task shall set completion timestamp and completed date.
- `Milestone 3` Completing the same task more than once shall be a successful no-op and shall not overwrite the original completion timestamp or completed date.
- `Milestone 5` Completing a task may award quest XP through the XP service.
- `V1` The system shall allow completed tasks to be displayed separately from active tasks.

### 3.3 Task deletion and archiving

- `V1` The system shall allow a task to be archived or soft-deleted; these are distinct operations.
- `V1` Archiving shall change `TaskItemStatus` to `Archived` without setting `IsDeleted`.
- `V1` Soft-deleted tasks shall not appear in normal task lists.
- `V1` Soft-deleting a task shall not remove historical XP transactions.

### 3.4 Task views

- `V1` The system shall display today's tasks.
- `V1` The system shall display overdue tasks.
- `V1` The system shall display active tasks.
- `Milestone 3` Active task views shall classify tasks as Overdue, Today, Upcoming, and Unscheduled. Unscheduled means no due date.
- `V1` The system shall provide basic filtering by status and due date.
- `Future` The system shall provide backlog, tag, and domain-specific views.
- `Future` The system shall provide time-block or calendar planning.

### 3.5 Recurring tasks and snooze

- `Future` The system shall support recurring tasks.
- `Future` The system shall generate task occurrences from recurrence rules.
- `Future` The system shall allow task snooze.
- `Future` The system shall allow task rescheduling.

## 4. Habit requirements

### 4.1 Habit creation and editing

- `V1` The system shall allow the user to create a habit.
- `V1` A habit shall have a name.
- `V1` A habit may have a description.
- `V1` A habit shall have a frequency.
- `V1` V1 shall support Daily as the only active habit frequency.
- `Milestone 4` New habits shall begin active. Archiving sets `IsActive = false`; archived habits are read-only and cannot be completed. Milestone 4 does not support restore/reactivate or user-facing habit deletion/soft deletion.
- `V1` A habit may have estimated time.
- `V1` A habit may have friction level.
- `Milestone 4` A habit may have target metadata: `TargetType = Binary`, or `TargetType = Quantity` with an optional target quantity and unit. Completion remains binary; users do not enter an achieved quantity.
- `Milestone 4` The system shall allow editing active habit fields. Archived habits are read-only.
- `Future` The system shall support selected-day, weekly, and monthly habit schedules. Future frequency enum values remain reserved and are not active Milestone 4 behavior.

### 4.2 Habit completion

- `Milestone 4` The system shall allow the user to complete an active daily habit for today's date in the user's time zone.
- `Milestone 4` A completion shall create one immutable HabitLog containing the user, habit, local completion date, and UTC completion instant.
- `Milestone 4` Completing the same habit/date more than once shall be a successful no-op. No second log is created.
- `Milestone 4` The database shall enforce uniqueness for `(UserId, HabitId, CompletionDate)` so concurrent duplicate requests resolve to the authoritative completed state rather than a user-visible duplicate-completion failure.
- `Milestone 5` Habit completion may award quest XP through the XP service. XP is not part of the Milestone 4 HabitService contract.

### 4.3 Habit streaks and statistics

- `Milestone 4` The system shall display a basic current streak for daily habits.
- `Milestone 4` The system shall display whether today's habit is completed.
- `Milestone 4` Streak calculation shall use distinct local completion dates. If today is complete, it anchors on today; otherwise yesterday may anchor the streak. If neither today nor yesterday is complete, the streak is zero. The calculation walks backward through consecutive dates, stops at the first missing date, and ignores future dates.
- `Future` The system shall calculate completion rate over selected periods.
- `Future` The system shall support momentum streaks.
- `Future` The system shall support weekly streaks.
- `Future` The system shall show daily, weekly, and monthly habit views.
- `Future` The system shall capture missed-day reasons or friction reasons.

## 5. Reminder and notification requirements

### 5.1 One-time reminders

 - `Milestone 6` The system shall allow the user to create a one-time reminder for a Task, Habit, or Custom source.
- `Milestone 6` A reminder shall have a local date/time entered by the user.
- `Milestone 6` The system shall convert the local date/time to UTC for storage.
- `Milestone 6` The system shall display reminder times back in the user's local time.
- `Milestone 6` A due reminder shall create an in-app notification.
- `Milestone 6` A reminder shall not create duplicate notifications if the reminder job retries.

### 5.2 Notifications

- `V1` The system shall display unread notifications.
- `V1` The system shall allow notifications to be marked as read or dismissed.
- `V1` Notifications shall be user-scoped.
 - `Milestone 6` The system shall create persisted notifications for reminder due events and committed level/echelon transitions.

### 5.3 Future reminder behavior

- `Future` The system shall support recurring reminders.
- `Future` The system shall support snooze.
- `Future` The system shall store reminder delivery history.
- `Future` The system shall support browser push notifications.
- `Future` The system may support email or mobile push notifications.

## 6. Gamification requirements

### 6.1 XP transaction log

- `V1` The system shall store every XP award as an XP transaction.
- `V1` The system shall not allow user-edited XP values.
- `V1` XP shall only be awarded by server-side services.
- `V1` Each XP transaction shall include user, source, amount, timestamp, and optional source entity reference.
- `V1` XP transactions shall be append-only except for administrative correction workflows added later.

### 6.2 Quest XP

- `V1` The system shall calculate quest XP from estimated time and friction level.
- `V1` Time base values shall be: `Under15Minutes = 50 XP`, `Between15And30Minutes = 100 XP`, `Between30And60Minutes = 150 XP`, `Over60Minutes = 200 XP`.
- `V1` Friction multipliers shall be: `Low = 1.0`, `Medium = 1.5`, `High = 2.0`.
- `V1` Quest XP shall use decimal arithmetic and, when multiplication produces a non-whole value, round to the nearest whole XP using `MidpointRounding.AwayFromZero`.
- `V1` The daily quest XP cap shall be 500 XP per user per user-local date.
- `Milestone 5` The system shall award quest XP when eligible tasks or habits are completed.
- `Milestone 5` The system shall not award duplicate quest XP for the same completion event.
- `Milestone 5` The system shall store an idempotency key for each XP award that represents a completion event.

### 6.3 User progression

- `V1` The system shall store total lifetime XP.
- `V1` The system shall calculate current level from total lifetime XP.
- `V1` XP required to advance from level `L` to level `L + 1` shall be `150 + (30 * L)`.
- `V1` Total XP required to reach level `N` shall be the sum of all advancement requirements from level 1 through level `N - 1`.
- `V1` The system shall calculate current echelon from current level.
- `V1` Echelon thresholds shall be: Iron 1-9, Bronze 10-19, Silver 20-29, Gold 30-39, Platinum 40-49, Onyx 50-74, Radiant 75-99, Apex 100-124, Celestial 125-149, Immortal 150-174, Abyssal 175-199, Ascendant 200+.
- `V1` The system shall display level and echelon on the dashboard or layout.
- `V1` The system shall update progression atomically with XP transaction creation.

### 6.4 Daily score and streak bonuses

- `V1` The system shall not implement the full DailyScore engine.
- `Future` The system shall calculate daily score from active/configured modules.
- `Future` The system shall exclude modules the user has not configured from the daily score denominator.
- `Future` The system shall award daily score XP through a scheduled job after the scoring model is trusted.
- `Future` The system shall award streak bonus XP through a scheduled job after streak rules are tested.

## 7. Dashboard requirements

- `Milestone 3` The dashboard shall display today's tasks and overdue tasks.
- `Milestone 4` The dashboard shall display today's habits and habit completion progress through a widget-specific capability.
- `Milestone 5` The dashboard shall display current XP, level, and echelon.
 - `Milestone 6` The header notification bell shall display the unread count; the Dashboard shall not add a notification widget.
- `V1` The dashboard shall display a simple finance monthly summary.
- `V1` The dashboard shall provide quick-add actions.
- `Future` The dashboard shall support sleep, workout, nutrition, study, wellbeing, and AI insight widgets.

## 8. Simple finance requirements

### 8.1 Transactions

- `V1` The system shall allow manual creation of income transactions.
- `V1` The system shall allow manual creation of expense transactions.
- `V1` A finance transaction shall have amount, date, type, category, and optional description.
- `V1` Transaction amount shall be a positive decimal value.
- `V1` Income/expense meaning shall come from transaction type, not negative amounts.
- `V1` The system shall allow editing manual transactions.
- `V1` The system shall allow deleting manual transactions; deleted records shall be excluded from ordinary history and calculations.
- `V1` The system shall store currency preference.

### 8.2 Categories

- `V1` The system shall provide default finance categories.
- `V1` The system shall provide stable global immutable default categories.
- `V1` User-created, renamed, or deleted categories are out of scope.

Default expense categories:

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

Default income categories:

- Salary;
- Other Income.

### 8.3 Monthly and yearly summaries

- `V1` The system shall calculate total income transactions for a selected month.
- `V1` The system shall calculate total expenses for a selected month.
- `V1` The system shall calculate monthly net cash flow as income transactions minus expense transactions.
- `V1` The system shall show spending by expense category for a selected month.
- `V1` The system shall calculate selected-year income, expenses, and net cash flow using transaction dates.
- `V1` Deleted transactions shall be excluded from all ordinary summaries.
- `V1` Historical transactions may be edited or deleted; no financial periods are locked.

### 8.4 Future finance possibilities

Future finance milestones may deliberately add richer personal-finance
analytics, trends, comparisons, simple budgets, savings goals, recurring
expenses, category management, imports, or AI summaries. None are M7
requirements, and no M7 schema is reserved for them.

## 9. M8–M12 domain requirements

The canonical planned requirements are in `16-post-m7-domain-roadmap.md`; M8's detailed architecture contract is `../engineering/16-m8-strength-training-architecture-contract.md`. M8 requires one shared strength/calisthenics exercise library, reusable templates, persisted active/historical sessions, mode-appropriate sets, warm-up/working distinction, substitutions/skips, prior performance, rest timer, feeling, and explicit completed/discarded lifecycle. Workout programming, RPE/RIR, custom exercises, and device data are deferred.

M9 requires domain-specific activity records, optional intensity, read-projected unified history, derived recalculable PRs, performance Fitness Goals, summaries, and a Fitness widget. It excludes GPS/device features, a duplicate timeline, cumulative/frequency Fitness Goals, and advanced analytics.

M10 requires practical Food/Food Log separation, seeded and custom foods, meal grouping, reusable meals, quick add, targets, hydration, simple supplements, summaries, and its widget. Barcode, external databases, dosage/pill tracking, prescription, and deep analytics are deferred.

M11 requires manual Health-owned body weight, sleep, and rapid structured wellbeing, with history and a Health widget. It excludes medical/vital/device features.

M12 requires Month/Week/Day views and native Calendar Events. Calendar projects, but never owns or duplicates, dated records from Tasks, Habits, Fitness, Nutrition, and Health. It does not schedule workouts, project ordinary Finance transactions, or require a Dashboard widget.

## 10. Study and project requirements

- `Future` The system shall allow study subjects to be defined.
- `Future` A study subject may have a weekly target.
- `Future` The system shall allow study session logging.
- `Future` Study sessions shall support duration, topic, method, and notes.
- `Future` The system shall allow personal projects to be defined.
- `Future` Projects shall support status, description, tech stack, GitHub link, and dates.
- `Future` The system shall allow work sessions to be logged against projects.
- `Future` The system shall calculate lifetime hours per project.
- `Future` The system shall surface neglected subjects or projects.

## 11. Focus session requirements

- `Future` The system shall provide a configurable Pomodoro timer.
- `Future` The system shall support focus duration, short break, and long break settings.
- `Future` Completed focus sessions shall auto-log study or project sessions.
- `Future` The system shall allow manual session logging without timer.

## 12. Wellbeing and journal requirements

- `M11` The system shall support the documented structured daily wellbeing check-in; it is not a journal or generic 1–5 score model.
- `Future` The system shall support a free-form daily journal entry.
- `Future` The system shall support optional guided prompts.
- `Future` The system shall support a quick field for what drained the user today.
- `Future` The system shall support weekly intention setting with three priorities.
- `Future` Weekly intentions may be reviewed by the AI assistant in a weekly review.

## 13. Analytics, AI, and integrations

- `M14` The system shall add deterministic analytics only after real historical data exists; the design is intentionally deferred.
- `Future` AI shall follow Analytics, has no milestone number, and shall consume structured data and deterministic capabilities rather than directly querying raw storage.
- `Future` Garmin, Strava, wearables, health platforms, device data, Google/Outlook/Teams Calendar, large food databases, barcode lookup, and related import/provenance/synchronization schemas require separately approved work.
- `Future` A cross-domain Accolades/Achievements system may recognize cumulative records; it is distinct from Fitness Goals.

## 14. Service boundary requirements

- `V1` Task workflows shall be exposed through a task service.
- `V1` Habit workflows shall be exposed through a habit service.
- `V1` Reminder workflows shall be exposed through a reminder service.
- `V1` Notification workflows shall be exposed through a notification service.
- `V1` XP workflows shall be exposed through an XP service.
- `V1` Finance workflows shall be exposed through a finance service.
- `V1` Razor components shall not directly mutate database state for these workflows.
- `V1` Service methods that mutate state shall return clear success/failure results or throw controlled domain/application exceptions that the UI can convert into user-facing messages.

## 19. Non-functional requirements

### 19.1 Performance

- `V1` Dashboard interactions should feel immediate.
- `V1` Common forms should load quickly.
- `Future` Chart-heavy views shall remain usable on desktop and acceptable on mobile.

### 19.2 Reliability

- `V1` Data entry shall not be fragile.
- `V1` Background jobs shall be observable and testable.
- `V1` Duplicate prevention shall be enforced by database constraints where required.

### 19.3 Usability

- `V1` Common actions should be available within one or two clicks/taps.
- `V1` Mobile layouts should prioritize quick entry.
- `V1` Empty states shall clearly guide the user.

### 19.4 Security and privacy

- `V1` All personal data shall be treated as sensitive.
- `V1` User data shall not be leaked across users.
- `Future` AI context shall be scoped to the current user and current request.

### 19.5 Maintainability

- `V1` The solution shall separate UI, domain, infrastructure, and job concerns.
- `V1` Feature logic shall live in services, not Razor pages.
- `V1` The system shall be modular enough to add future domains.

### 18.1 Milestone 5 XP contract

Milestone 5 uses `IXpService`/`XpService` and `IXpRepository`. It awards positive `QuestCompletion` XP for new Task and Habit completion events only, uses the shared 500-XP per-user-local-business-date cap, and treats cap crossing, cap exhaustion, duplicate replay, and lazy progression initialization as successful handled outcomes. Positive actual awards are recorded in an append-only ledger and committed atomically with the materialized progression projection. Level/echelon transitions are returned as metadata; persisted notifications and notification UI are Milestone 6. The XP Progress Dashboard widget is in scope, while a global/header XP chip and XP history UI are not.
