# 05 - UX Flow / Wireframe Document

## 1. Purpose

This document describes how the user moves through LifeOS. It defines V1 screens and preserves future module flows so they are not lost.

The wireframes are textual. They are intended to guide implementation before high-fidelity design.

## 2. UX direction

LifeOS should feel like a clean JARVIS-inspired command center:

- dark charcoal base;
- cyan/teal/blue accents;
- card-based dashboard;
- low clutter;
- high contrast;
- strong hierarchy;
- quick actions;
- desktop-first analytics;
- mobile-first entry flows.

## 3. Navigation model

### 3.1 V1 navigation

Primary navigation:

- Dashboard
- Tasks
- Habits
- Reminders/Notifications
- Finance
- Settings

Secondary navigation or header elements:

- XP Progress widget (Dashboard only; no global/header XP chip in Milestone 5)
- Notification bell
 - Notification bell
- Profile/settings menu

### 3.2 Planned and deferred navigation

The approved M8-M12 navigation modules are:

- Fitness;
- Nutrition;
- Health;
- Calendar.

Journal, broader Body Metrics, Study, Projects, AI, and Reports/Insights are future concepts. They are not planned navigation modules in M8-M14.

Navigation should support module groups so the sidebar does not become too long.

## 4. First-run setup flow

Purpose: prevent core features from working with missing assumptions.

First-run fields:

- display name;
- time zone, defaulting to `UTC` until the user explicitly confirms a valid time zone;
- default currency;

Rules:

- reminders should not be enabled until time zone is configured;
- finance dashboard should show a helpful empty state until a transaction exists;
- seed data should be development-only unless explicitly enabled.

## 5. V1 dashboard wireframe

Desktop layout:

```text
+--------------------------------------------------------------+
| Header: LifeOS | Notification bell                    |
+----------------------+----------------------+----------------+
| Today's Tasks        | Today's Habits       | XP Progress    |
| - due task           | - habit checkboxes   | level bar      |
| - overdue task       | completion percent   | echelon badge  |
+----------------------+----------------------+----------------+
| Reminders            | Finance Snapshot     | Quick Actions  |
| - next 3 reminders   | income/expense/bal   | task/habit/tx  |
+----------------------+----------------------+----------------+
```

Mobile layout:

```text
Dashboard
XP Progress widget
Today tasks card
Today habits card
Finance card
```

Dashboard rules:

- do not show empty future modules in V1;
- show helpful empty states;
- prioritize what needs action today;
- avoid visual clutter;
- make completion actions one tap/click.

## 6. V1 task flow

### 6.1 Task list page

Purpose: manage active and completed tasks.

Sections:

- Today
- Overdue
- Upcoming
- Unscheduled
- Completed and Archived as separate status views

Actions:

- Add task
- Edit task
- Complete task
- Delete/archive task
- Filter by status/date/category

Empty state:

- "No tasks for today. Add one or enjoy the clear board."

### 6.2 Add/edit task form

Fields:

- Title
- Description/notes
- Due date
- Due time optional
- Priority
- Category/domain
- Estimated time
- Friction level

After save:

- return to previous list or dashboard;
- show toast/notification;
- if reminder added, show local display time.

### 6.3 Complete task interaction

Flow:

1. User clicks complete.
2. UI disables button during request.
3. Task service marks task complete.
4. UI updates the task list.

Milestone 3 has no XP or reminder integration. XP is introduced in Milestone 5 through the Dashboard XP Progress widget and completion feedback; persisted notifications and the notification UI belong to Milestone 6.

Failure behavior:

- show error;
- do not visually complete unless persistence succeeds.

## 7. V1 habit flow

### 7.1 Habit list page

Purpose: manage habit definitions.

Sections:

- Active habits
- Archived habits

Actions:

- Add habit
- Edit habit
- Archive habit
- View history

Archived habits remain visible in history, are read-only, and cannot be completed. Milestone 4 does not provide restore/reactivate or delete actions.

### 7.2 Today habits card

Each habit row:

```text
[checkbox] Habit name | streak | completion state | target info
```

Interaction:

- clicking checkbox logs today's completion;
- duplicate click does not create duplicate log;
- completed state is visually clear;
- completion remains binary even when the Habit definition has a quantity target;
- users do not enter an achieved quantity during completion.

### 7.3 Add/edit habit form

Fields:

- Name
- Description
- Frequency: Daily in V1
- Target type: binary or quantity
- Optional target quantity and unit metadata
- Estimated time
- Friction level

Empty state:

- "No habits yet. Start with one habit you can realistically do today."

## 8. V1 reminder and notification flow

### 8.1 Create one-time reminder

Entry points:

- task form;
- habit form;
- reminder page;
- reminders page (global Quick Add is future scope).

Fields:

- title/message;
- associated Task, Habit, or Custom source;
- local date/time;
- explicitly confirmed scheduling time zone display.

Save behavior:

- local date/time converted to UTC;
- confirmation displays local time;
- pending reminder appears on reminder list.

### 8.2 Reminder processing

User-facing flow:

1. Reminder due time arrives.
2. Background job creates in-app notification.
3. Notification bell shows unread count.
4. User opens notification list.
5. User marks read/dismisses.

### 8.3 Notification list

Items display:

- title;
- message;
- created local time;
- source link if available;
- mark-read/dismiss action; dismissed items are absent from the default list.

Reminder snooze, recurring controls, browser push, and external delivery are
future scope. Reminder notifications link to the reminder and expose a source
link only when the current-user source remains safely available.

## 9. V1 finance flow

### 9.1 Finance dashboard page

Purpose: simple monthly money awareness.

Sections:

- selected month;
- total income;
- total expenses;
- net cash flow;
- category breakdown;
- recent transactions.

No advanced import UI in V1.

### 9.2 Add transaction form

Fields:

- Type: income or expense;
- Amount;
- Date;
- Category;
- Description optional.

After save:

- monthly summary updates;
- transaction appears in recent list.

### 9.3 Year summary flow

User can select a year and see total income, total expenses, and net cash flow.
The year summary is a small aggregate, not an analytics dashboard.

## 10. V1 settings flow

Settings sections:

- profile;
- time zone;
- currency;
- theme preference;
- data reset/export future placeholder if desired.

Time zone setting must be obvious because reminders depend on it.

## 11. Planned M8–M12 UX flows

Detailed planned requirements are canonical in `16-post-m7-domain-roadmap.md`.

### 11.1 Fitness

Fitness uses a dedicated active-workout logger rather than generic CRUD: choose a template or Custom Workout, log mode-appropriate sets, see prior performance, use a rest timer, substitute/skip exercises, complete with feeling and summary, then review historical sessions/exercise history. M9 extends the same landing experience with activity entry, PRs, Goals, and unified history. It is not a workout-programming UI.

### 11.2 Nutrition and Health

Nutrition Today combines daily macros, meal-grouped food logs, hydration, supplements, targets, recent/favourites, reusable meals, and Quick Add. Health Today supports rapid weight, sleep, and four-value wellbeing capture; historical review is separate. Both use compact domain-projection Dashboard widgets and avoid medical, barcode, or device workflows.

### 11.3 Calendar

Calendar defaults to Month and also provides Week/Day. It creates only Events and delegates `+ Task` to the existing task flow with the selected date. Dated Tasks/Habits and recorded Fitness/Nutrition/Health data appear as projections with domain navigation; Calendar does not create workouts, meals, or health data, schedule workouts, or show ordinary Finance transactions.

### 11.4 M13, Analytics, and AI

M13 reviews and polishes the above operational flows as one product. M14 analytics follows real data accumulation. AI is future work after Analytics, not an immediate UI flow.

## 12. Mobile behavior

Mobile should prioritize:

- quick task add;
- habit checkoff;
- transaction add;
- notification read;
- daily check-in in future.

Mobile should not prioritize dense charts until later.

## 13. Empty, loading, and error states

Every V1 page should define:

- empty state;
- loading state;
- validation errors;
- save failure state;
- unauthorized state where applicable.

Examples:

- no tasks: suggest adding one task;
- no habits: suggest starting with one habit;
- no finance transactions: suggest adding a first income or expense;
- reminder conversion error: ask user to confirm time zone.
