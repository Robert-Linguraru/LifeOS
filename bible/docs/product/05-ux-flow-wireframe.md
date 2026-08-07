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

- XP/Level chip
- Notification bell
- Quick add button
- Profile/settings menu

### 3.2 Future navigation

Future modules may add:

- Sleep/Health
- Fitness
- Body Metrics
- Nutrition
- Study
- Projects
- Wellbeing/Journal
- AI Assistant
- Reports/Insights

Navigation should support module groups so the sidebar does not become too long.

## 4. First-run setup flow

Purpose: prevent core features from working with missing assumptions.

First-run fields:

- display name;
- time zone, defaulting to Europe/Bucharest unless changed;
- default currency;
- planned monthly allowance or expected monthly income.

Rules:

- reminders should not be enabled until time zone is configured;
- finance dashboard should show a helpful empty state until a monthly plan or transaction exists;
- seed data should be development-only unless explicitly enabled.

## 5. V1 dashboard wireframe

Desktop layout:

```text
+--------------------------------------------------------------+
| Header: LifeOS | Level/Echelon | Quick Add | Notifications   |
+----------------------+----------------------+----------------+
| Today's Tasks        | Today's Habits       | XP Progress    |
| - due task           | - habit checkboxes   | level bar      |
| - overdue task       | completion percent   | echelon badge  |
+----------------------+----------------------+----------------+
| Reminders            | Finance Snapshot     | Quick Actions  |
| - next reminders     | income/expense/bal   | task/habit/tx  |
+----------------------+----------------------+----------------+
```

Mobile layout:

```text
Header
XP chip
Today tasks card
Today habits card
Quick add buttons
Finance card
Notifications card
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

Milestone 3 has no XP or reminder integration. Those interactions are introduced only with their Milestone 5 and 6 slices.

Failure behavior:

- show error;
- do not visually complete unless persistence succeeds.

## 7. V1 habit flow

### 7.1 Habit list page

Purpose: manage habit definitions.

Sections:

- Active habits
- Inactive/archived habits

Actions:

- Add habit
- Edit habit
- Archive habit
- View history

### 7.2 Today habits card

Each habit row:

```text
[checkbox] Habit name | streak | XP preview | target info
```

Interaction:

- clicking checkbox logs today's completion;
- duplicate click does not create duplicate log;
- completed state is visually clear;
- XP updates if eligible.

### 7.3 Add/edit habit form

Fields:

- Name
- Description
- Frequency: Daily in V1
- Target type: binary or quantity
- Target quantity and unit
- Estimated time
- Friction level
- Active status

Empty state:

- "No habits yet. Start with one habit you can realistically do today."

## 8. V1 reminder and notification flow

### 8.1 Create one-time reminder

Entry points:

- task form;
- habit form;
- reminder page;
- quick add.

Fields:

- title/message;
- associated task/habit optional;
- local date/time;
- time zone display.

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
- read/dismiss action.

Future actions:

- snooze;
- open related object;
- recurring reminder controls;
- browser push permission.

## 9. V1 finance flow

### 9.1 Finance dashboard page

Purpose: simple monthly money awareness.

Sections:

- selected month;
- planned monthly allowance/income;
- total income;
- total expenses;
- remaining planned balance;
- category breakdown;
- recent transactions.

No advanced import UI in V1.

### 9.2 Add transaction form

Fields:

- Type: income or expense;
- Amount;
- Currency;
- Date;
- Category;
- Description;
- Notes optional.

After save:

- monthly summary updates;
- transaction appears in recent list.

### 9.3 Monthly plan flow

User can set:

- planned monthly allowance/income;
- optional expense target.

The dashboard shows:

- manual income transactions;
- spent so far;
- remaining planned balance;
- simple category totals.

## 10. V1 settings flow

Settings sections:

- profile;
- time zone;
- currency;
- monthly allowance;
- theme preference;
- data reset/export future placeholder if desired.

Time zone setting must be obvious because reminders depend on it.

## 11. Future module UX flows

### 11.1 Sleep and health flow

Future screens:

- Sleep log
- Sleep trends
- Health markers

Daily sleep form:

- sleep date;
- bedtime;
- wake time;
- quality;
- energy;
- notes.

Dashboard widget:

- last night's sleep;
- weekly average;
- target status.

### 11.2 Fitness and progressive overload flow

Future screens:

- Workout plans
- Active plan
- Start workout
- Session logger
- Exercise library
- Lift history

Workout session wireframe:

```text
Workout: Push Day
Exercise 1: Bench Press
Set | Target | Actual Reps | Weight | Status
1   | 8      | [ ]         | [ ]    | Completed/Failed/Skipped
2   | 8      | [ ]         | [ ]    | Completed/Failed/Skipped
Rest timer
Notes
Finish Session
```

Progressive overload view:

- exercise selector;
- weight trend;
- volume trend;
- PR markers;
- stall warnings.

### 11.3 Body metrics and physique flow

Future screens:

- Body metrics log
- Measurement trends
- Progress photo timeline
- Phase settings

Entry flow:

- date;
- weight;
- measurements;
- body fat optional;
- phase;
- photo optional.

Dashboard widget:

- current weight;
- weekly change;
- phase;
- trend indicator.

### 11.4 Nutrition and meal prep flow

Future screens:

- Daily nutrition
- Add meal
- Meal templates
- Meal prep planner
- Targets

Daily nutrition wireframe:

```text
Protein: 120 / 160g
Calories: 2100 / 2500 kcal
Water: 1800 / 2500 ml
Meals:
- Breakfast
- Lunch
- Dinner
Quick add template
```

Meal prep planner:

- week selector;
- planned meals;
- portions;
- target days;
- planned versus actual.

### 11.5 Study and project flow

Future screens:

- Study subjects
- Study session log
- Projects
- Project work log
- Focus timer

Study flow:

1. Select subject.
2. Start focus timer or manual session.
3. Log topic and method.
4. Session adds to weekly target.

Project flow:

1. Select project.
2. Start work session.
3. Log notes and duration.
4. Project lifetime hours update.

### 11.6 Pomodoro/focus flow

Future timer states:

- ready;
- focusing;
- short break;
- long break;
- completed;
- abandoned.

Timer completion creates a study or project session if linked.

### 11.7 Wellbeing and journal flow

Future daily check-in:

```text
Mood:   1 2 3 4 5
Energy: 1 2 3 4 5
Stress: 1 2 3 4 5
What drained me today? [optional]
Journal [optional]
```

Weekly intentions:

- Monday: set three priorities;
- dashboard: display priorities;
- Sunday: review alignment.

### 11.8 AI assistant flow

Future AI screens:

- AI chat;
- weekly review;
- insight inbox;
- module reports.

AI response UX rules:

- show time period used;
- show data basis;
- show confidence;
- distinguish suggestion from fact;
- provide links to relevant records where safe.

### 11.9 Advanced finance flow

Future advanced finance screens:

- import transactions;
- import preview;
- duplicate review;
- budget setup;
- subscription manager;
- savings goals;
- net worth snapshots;
- AI finance summary.

Import preview flow:

1. Upload file.
2. Parser extracts rows.
3. User reviews category suggestions.
4. Duplicates are highlighted.
5. User confirms import.
6. Transactions are created.

### 11.10 Garmin integration flow

Future Garmin flow:

1. User exports data from Garmin Connect.
2. User imports CSV into LifeOS.
3. App previews parsed sleep/workout/recovery records.
4. User confirms import.
5. Manual and imported records remain distinguishable.

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
- no finance transactions: suggest adding monthly allowance or first expense;
- reminder conversion error: ask user to confirm time zone.
