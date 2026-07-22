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

# Task Enums

## TaskStatus

```csharp
public enum TaskStatus
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

---

## FrictionLevel

```csharp
public enum FrictionLevel
{
    Low = 0,
    Medium = 1,
    High = 2
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

---

## HabitTargetType

```csharp
public enum HabitTargetType
{
    Binary = 0,
    Quantity = 1
}
```

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
    System = 3,
    FutureInsight = 4
}
```

---

# XP Enums

## XPSource

```csharp
public enum XPSource
{
    QuestCompletion = 0,
    DailyScore = 1,
    StreakBonus = 2,
    ManualAdjustment = 3,
    System = 4
}
```

---

## XPSourceType

```csharp
public enum XPSourceType
{
    Task = 0,
    Habit = 1,
    DailyScore = 2,
    Streak = 3
}
```

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

---

## FinanceSource

```csharp
public enum FinanceSource
{
    Manual = 0,
    Imported = 1
}
```

---

# Future Fitness Enums

## WorkoutPhase

```csharp
public enum WorkoutPhase
{
    Bulk = 0,
    Cut = 1,
    Maintain = 2
}
```

---

## WorkoutDayType

```csharp
public enum WorkoutDayType
{
    Push = 0,
    Pull = 1,
    Legs = 2,
    Upper = 3,
    Lower = 4,
    Rest = 5,
    Custom = 6
}
```

---

## ExerciseEquipment

```csharp
public enum ExerciseEquipment
{
    Barbell = 0,
    Dumbbell = 1,
    Machine = 2,
    Cable = 3,
    Bodyweight = 4,
    ResistanceBand = 5,
    Other = 6
}
```

---

## SetStatus

```csharp
public enum SetStatus
{
    Completed = 0,
    Failed = 1,
    Skipped = 2
}
```

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