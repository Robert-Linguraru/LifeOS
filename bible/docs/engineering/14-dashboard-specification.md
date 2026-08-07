# Dashboard Specification

## Purpose

The Dashboard is the primary entry point into LifeOS.

It acts as the user's daily command center by surfacing information from modules that have been implemented.

The dashboard should answer one question:

> **"What should I focus on right now?"**

It is **not** intended to expose every available feature or every stored piece of data.

---

# Design Principles

The dashboard must be:

- Fast
- Actionable
- Minimal
- Information-dense
- Easy to scan
- Responsive
- Modular
- Expandable

Priority is always given to **today's actionable information** over historical analytics.

---

# Dashboard Layout

```
--------------------------------------------------------------
 Header
--------------------------------------------------------------
 Logo        Profile
--------------------------------------------------------------

Today's Tasks      Quick Actions

--------------------------------------------------------------

Future Widgets (when enabled)

Sleep
Workout
Nutrition
Body Metrics
Study
Projects
Wellbeing
AI Insights

--------------------------------------------------------------
```

---

# Header

Contains

- Application logo
- Current page title
- Search (future)
- XP display (Milestone 5)
- Notification bell (Milestone 6)
- User profile
- Settings shortcut

---

# Dashboard Sections

## Today's Tasks

Purpose

Display the tasks requiring attention today.

Displays

- Due Today
- Overdue
- Recently Added
- Completion status

Actions

- Complete
- Open
- Edit
- Add Task

Empty State

```
No tasks due today.

Enjoy the clear board or add a new task.
```

---

## Future widgets

The following sections are introduced only with their owning module milestones. They are not current DashboardService responsibilities and must not be predeclared as methods before those slices begin.

### Today's Habits

Purpose

Display habits due today.

Displays

- Habit name
- Current streak
- Completion state
- Estimated XP

Actions

- Complete Habit
- Open Habit
- Add Habit

Empty State

```
No habits configured.

Start with one habit you can realistically complete today.
```

---

## XP Progress

Purpose

Display current progression.

Displays

- Current Level
- Current Echelon
- Total XP
- Today's Quest XP
- Daily XP Cap Progress

Actions

- View Progress (future)

---

## Reminders

Purpose

Display upcoming reminders.

Displays

- Next reminder
- Due time
- Related Task/Habit
- Reminder status

Actions

- Open
- Dismiss Notification

Future

- Snooze
- Recurring reminders

---

## Finance Snapshot

Purpose

Provide quick monthly financial awareness.

Displays

- Expected Income / Allowance
- Total Income
- Total Expenses
- Remaining Balance
- Largest Spending Category

Actions

- Add Expense
- Add Income
- Open Finance

Empty State

```
No transactions recorded this month.

Add your first transaction.
```

---

## Quick Actions

Purpose

Reduce navigation.

Buttons

- Add Task
- Add Habit
- Add Transaction
- Add Reminder

Future

- Start Workout
- Log Meal
- Log Sleep
- Start Focus Session

---

# Dashboard Refresh Rules

The dashboard should refresh automatically after:

- Task completion
- Habit completion
- Reminder firing
- Notification dismissal
- Finance transaction creation
- XP award
- User progression update

Future modules should refresh only their affected widgets.

---

# Dashboard Data Source

At Milestone 3, Dashboard data is aggregated exclusively by **DashboardService** from task data only.

```
Dashboard

        │

        ▼

DashboardService

        │

        └──────── TaskService

                │

                ▼

           DashboardDto
```

Dashboard pages must never query repositories or DbContext directly.

---

# Dashboard DTO

DashboardService returns a single DashboardDto.

The DashboardDto should contain:

- Today's Tasks
- Task summary

Future module slices may extend DashboardDto and DashboardService when their underlying service exists.

---

# Widget Design Rules

Every widget shall contain:

- Title
- Primary information
- Secondary information
- Primary action
- Empty state
- Loading state
- Error state

Widgets must never depend on another widget.

Each widget should be independently replaceable.

---

# Future Dashboard Widgets

These widgets are introduced only when their module is implemented.

## Sleep

Displays

- Last night's sleep
- Weekly average
- Sleep target

---

## Workout

Displays

- Today's workout
- Last session
- Current program
- Next workout

---

## Nutrition

Displays

- Calories
- Protein
- Water
- Daily targets

---

## Body Metrics

Displays

- Current weight
- Weekly change
- Current phase

---

## Study

Displays

- Weekly target
- Today's focus
- Subject progress

---

## Projects

Displays

- Active projects
- Recent work
- Hours this week

---

## Wellbeing

Displays

- Mood
- Energy
- Stress
- Weekly intention

---

## AI Insights

Displays

- Weekly insight
- Suggested focus
- Recent trend
- Confidence level

---

# Mobile Dashboard

The mobile dashboard prioritizes speed over analytics.

Order

1. Quick Actions
2. Today's Tasks
3. Today's Habits
4. XP Progress
5. Reminders
6. Finance Snapshot
7. Notifications

Future widgets appear below the core modules.

---

# Dashboard Performance Rules

- Initial load should require a single DashboardService call.
- Widgets should not independently query the database.
- Dashboard should minimize unnecessary refreshes.
- Heavy analytics are future scope and should be loaded separately.

---

# Business Rules

- Dashboard is read-only.
- Dashboard owns no business logic.
- Dashboard aggregates information only.
- Dashboard never writes directly to the database.
- Dashboard always consumes DTOs.
- Future modules integrate by extending DashboardService and DashboardDto, never by querying the database directly from the UI.