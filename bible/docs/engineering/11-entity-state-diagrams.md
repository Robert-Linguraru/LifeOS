# Entity State Diagrams

## Purpose

This document defines the lifecycle of the primary entities within LifeOS.

Each diagram represents the valid states an entity may enter during its lifetime.

These state diagrams define:

- valid lifecycle transitions;
- allowed state changes;
- invalid transitions;
- future expansion points.

These diagrams describe business behaviour only and do not dictate implementation.

---

# Task Lifecycle

```
Created
    │
    ▼
Active
 ├───────────────┬───────────────┐
 │               │               │
 ▼               ▼               ▼
Completed    Archived      Soft Deleted
```

### Rules

- Every task begins in **Created**.
- Saving the task moves it to **Active**.
- Active tasks may be completed.
- Active tasks may be archived.
- Completing an already-completed task is a successful no-op; it does not overwrite `CompletedAtUtc` or `CompletedDate`.
- Completed and archived tasks are read-only.
- Archiving changes `TaskItemStatus` to `Archived`; archived tasks are available through explicit archived-task views and are hidden from active views.
- Deletion performs EF deletion that `AppDbContext` converts into a soft delete; soft-deleted tasks are hidden by normal query filters.
- Milestone 3 does not support restore or reopening: completed and archived tasks cannot return to Active.
- Recurring tasks introduce additional states in a future version.

---

# Habit Lifecycle

```
Created
    │
    ▼
Active
    │
    ▼
Completed Today
    │
    ▼
Waiting For Next Day
    │
    └──────────────► Active
```

### Rules

- Habits remain active indefinitely until archived.
- Completing a habit creates a HabitLog.
- A daily habit may only be completed once per day.
- At the beginning of the next local day, the habit becomes available again.
- Future measurable habits may support multiple completions.

---

# Habit Log Lifecycle

```
Created
    │
    ▼
Stored
```

### Rules

- Habit logs are immutable after creation.
- Editing a completion is future scope.
- Deleting a completion is administrative only.

---

# Reminder Lifecycle

```
Created
    │
    ▼
Pending
    │
    ▼
Triggered
    │
    ▼
Notification Created
    │
    ├────────────► Read
    │
    └────────────► Dismissed
```

### Rules

- Pending reminders wait until the scheduled UTC instant.
- Triggering a reminder creates exactly one notification.
- Triggering must be idempotent.
- Future recurring reminders create new reminder occurrences rather than resetting the existing reminder.

---

# Notification Lifecycle

```
Created
    │
    ▼
Unread
 ├──────────┐
 ▼          ▼
Read    Dismissed
```

### Rules

- Notifications are created only by services.
- Users may read or dismiss notifications.
- Notifications are never edited.
- Future notification channels do not change the notification lifecycle.

---

# XP Transaction Lifecycle

```
Generated
    │
    ▼
Persisted
```

### Rules

- XP Transactions are append-only.
- XP Transactions are never modified.
- Corrections create compensating transactions rather than editing history.

---

# User Progression Lifecycle

```
Seeded
    │
    ▼
Active
    │
    ▼
Updated
```

### Rules

- Every user owns exactly one UserProgression record.
- Progression is updated only through XPService.
- Progression is never edited manually.

---

# Finance Transaction Lifecycle

```
Created
    │
    ▼
Active
    │
    ├────────────► Updated
    │
    └────────────► Archived
```

### Rules

- Finance transactions may be edited.
- Archived transactions remain in historical reporting if required.
- Imported transactions follow the same lifecycle.

---

# Monthly Finance Plan Lifecycle

```
Created
    │
    ▼
Active
    │
    ▼
Updated
```

### Rules

- One monthly plan exists per user per month.
- Updating a plan never modifies historical months.

---

# Future Entity Lifecycles

Future modules should define their own lifecycle diagrams before implementation.

These include:

- SleepEntry
- WorkoutSession
- BodyMetricEntry
- MealEntry
- StudySession
- Project
- FocusSession
- DailyWellbeing
- WeeklyIntention
- AIConversation
- ImportBatch

No future module should be implemented until its lifecycle has been documented.