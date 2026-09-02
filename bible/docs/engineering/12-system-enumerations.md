# System Enumerations

## Purpose

This document defines every enumeration used throughout LifeOS.

The objectives are to:

- establish a single source of truth for enums;
- prevent duplicate or inconsistent enum definitions;
- improve consistency across the application;
- ensure GitHub Copilot generates the correct values;
- reduce refactoring during development.

All enums shall be defined in **LifeOS.Core/Enums**.

---

# Shared Effort Enums

These enums are shared domain concepts used by Tasks and future Habits. They live directly under `LifeOS.Core.Enums`, not under the Task-specific namespace.

## EstimatedTime

```csharp
public enum EstimatedTime
{
    Under15Minutes = 0,
    Between15And30Minutes = 1,
    Between30And60Minutes = 2,
    Over60Minutes = 3
}
```

## FrictionLevel

```csharp
public enum FrictionLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}
```

The numeric values are stable and must not be reordered because they are persisted by EF Core as integers.

---

# Task Enums

## TaskItemStatus

```csharp
public enum TaskItemStatus
{
    Active = 0,
    Completed = 1,
    Archived = 2
}
```

---

## TaskPriority

```csharp
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

---

## TaskCategory

```csharp
public enum TaskCategory
{
    Personal = 0,
    School = 1,
    Health = 2,
    Finance = 3,
    Admin = 4,
    Work = 5,
    Fitness = 6,
    Miscellaneous = 7
}
```

---

# Habit Enums

## HabitFrequency

```csharp
public enum HabitFrequency
{
    Daily = 0,
    SelectedDays = 1,
    Weekly = 2,
    Monthly = 3
}
```

The numeric values are stable and must not be reordered. Milestone 4 accepts only `Daily`; `SelectedDays`, `Weekly`, and `Monthly` are reserved for future schedule behavior and must not be exposed as active Milestone 4 options.

---

## HabitTargetType

```csharp
public enum HabitTargetType
{
    Binary = 0,
    Quantity = 1
}
```

Milestone 4 uses `HabitTargetType` as definition metadata. A `Quantity` target does not add achieved-quantity entry to the binary completion event.

---

# Reminder Enums

## ReminderStatus

```csharp
public enum ReminderStatus
{
    Pending = 0,
    Fired = 1,
    Cancelled = 2
}
```

---

## ReminderSourceType

```csharp
public enum ReminderSourceType
{
    Task = 0,
    Habit = 1,
    Custom = 2
}
```

---

# Notification Enums

## NotificationType

```csharp
public enum NotificationType
{
    ReminderDue = 0,
    LevelUp = 1,
    EchelonChanged = 2,
}
```

```csharp
public enum NotificationSourceType
{
    Reminder = 0,
    XpTransaction = 1
}
```

---

# XP Enums

## XpSource

```csharp
public enum XpSource
{
    QuestCompletion = 0,
    DailyScore = 1,
    StreakBonus = 2,
    ManualAdjustment = 3,
    System = 4
}
```

---

## XpSourceType

```csharp
public enum XpSourceType
{
    Task = 0,
    Habit = 1,
    DailyScore = 2,
    Streak = 3
}
```

The numeric values are stable and must not be renumbered because they are persisted by EF Core. Milestone 5 uses `QuestCompletion` with `Task` or `Habit` source types. `DailyScore`, `StreakBonus`, `ManualAdjustment`, `System`, and future source types remain reserved; their behavior is not implemented in Milestone 5. The planned .NET symbols use `XpSource`, `XpSourceType`, and `XpTransaction`, not `XPSource` or `XPSourceType`.

---

## Echelon

```csharp
public enum Echelon
{
    Iron = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Onyx = 5,
    Radiant = 6,
    Apex = 7,
    Celestial = 8,
    Immortal = 9,
    Abyssal = 10,
    Ascendant = 11
}
```

---

# Finance Enums

## FinanceTransactionType

```csharp
public enum FinanceTransactionType
{
    Income = 0,
    Expense = 1
}
```

---

## FinanceCategoryType

```csharp
public enum FinanceCategoryType
{
    Income = 0,
    Expense = 1,
    Both = 2
}
```

`FinanceSource` and imported-record enumerations are not part of M7. M7
transactions are manual by definition.

---

# Planned M8–M12 Enumerations

These are requirements concepts, not implemented symbols or a final persistence contract. Their precise names and numeric values are decided only in the owning milestone and then added here before implementation.

- M8: Exercise equipment includes Barbell, Dumbbell, Cable, Machine, Smith Machine, Bodyweight, Pull-up Bar, Dip Bars, Kettlebell, Resistance Band, and Other; logging modes are Weight + Reps, Bodyweight + Reps, Added Weight + Reps, Assisted Weight + Reps, Reps Only, Duration, and Weight + Duration; set kind is Warm-up or Working; workout lifecycle includes In Progress, Completed, and Discarded; Session Feeling is Weak, Normal, Good, or Great.
- M9: activity subtype/category and optional Perceived Intensity (Easy, Moderate, Hard, Max Effort) are defined by the activity requirements; the system must not default missing intensity.
- M10: meal grouping is Breakfast, Lunch, Dinner, Snack, and optional Other; supplements are Taken/Not Taken.
- M11: Sleep Quality is Poor, Okay, Good, Great; Energy is Low, Normal, High, Very High; Mood/Overall are Poor, Okay, Good, Great; Stress is Low, Moderate, High, Very High.
- M12: Calendar views are Month, Week, Day; Month is the default.

See `16-post-m7-domain-roadmap.md` for normative planned behavior. Do not introduce RPE/RIR, generic health metric, import/provenance, or AI enums in M8–M12.

---

# Future Study Enums

## StudyMethod

```csharp
public enum StudyMethod
{
    Pomodoro = 0,
    DeepWork = 1,
    Review = 2,
    Lecture = 3,
    Other = 4
}
```

---

## ProjectStatus

```csharp
public enum ProjectStatus
{
    Idea = 0,
    InProgress = 1,
    Paused = 2,
    Shipped = 3
}
```

---

# Future AI Enums

## AIMessageRole

```csharp
public enum AIMessageRole
{
    User = 0,
    Assistant = 1,
    System = 2,
    Tool = 3
}
```

---

## InsightType

```csharp
public enum InsightType
{
    WeeklyReview = 0,
    Suggestion = 1,
    Correlation = 2,
    FinanceSummary = 3,
    PhysiqueReport = 4,
    StudySummary = 5
}
```

---

## InsightConfidence

```csharp
public enum InsightConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}
```

---

# Rules

- Enums must never be duplicated.
- Enums must be shared across all layers through `LifeOS.Core`.
- New enums require documentation updates before implementation.
- Existing enum values must not be reordered after production data exists.
- Deprecated enum values should be marked obsolete rather than removed when backwards compatibility is required.