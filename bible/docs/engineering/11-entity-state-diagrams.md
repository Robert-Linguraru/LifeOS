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
Archived
```

### Rules

- New Habits begin **Active**.
- Active Habits may be updated, completed for the current user-local date, or archived.
- Archiving sets `IsActive = false`.
- Archived Habits remain persisted and available for history.
- Archived Habits are read-only and cannot be completed.
- Milestone 4 has no restore/reactivate transition and no user-facing Habit delete or soft-delete operation.
- Future account/admin lifecycle behavior is outside Milestone 4.

---

# Habit Log Lifecycle

```
Completion Requested
        │
        ▼
Stored Once
```

### Rules

- A completion is a binary event for one Habit and one user-local `CompletionDate`.
- HabitLogs are immutable after creation.
- The unique key `(UserId, HabitId, CompletionDate)` prevents duplicate rows.
- Repeating the same completion is a successful no-op that preserves the original log.
- Concurrent duplicate requests resolve to the authoritative stored completion.
- Editing, deleting, backdating, quantity entry, and XP metadata are outside Milestone 4.

---

# Reminder Lifecycle

```
Pending ─────────────► Fired
   │
   └──────────────────► Cancelled
```

### Rules

- Pending reminders wait until the scheduled UTC instant.
- Firing a reminder creates exactly one notification in the same transaction.
- Firing must be idempotent.
- Fired and Cancelled are terminal; recurring reminders are outside Milestone 6.

# Notification Lifecycle

```
Unread ─────────────► Read
   │                     │
   └─────────────────────┴──► Dismissed
```

### Rules

- Notifications are created only by services.
- Mark-read is idempotent.
- Dismiss is idempotent and terminal; dismissal also sets ReadAtUtc when needed.
- Dismissed notifications are excluded from the default list and unread count.
- There are no external-delivery states in Milestone 6.

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
- Existing rows cannot be modified or deleted. Future corrections may use compensating transactions; no correction workflow is in Milestone 5.

---

# User Progression Lifecycle

```
Lazily initialized
    │
    ▼
Active
    │
    ▼
Updated
```

### Rules

- Every user owns exactly one UserProgression record.
- Progression is updated only through `XpService`.
- Progression is never edited manually.
- First access or first award creates the row race-safely; no startup or account seeding is required.

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