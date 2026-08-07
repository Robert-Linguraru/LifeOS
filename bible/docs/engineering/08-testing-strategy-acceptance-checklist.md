# 08 - Testing Strategy / Acceptance Checklist

## 1. Purpose

This document defines how LifeOS will be tested. It focuses on the rules most likely to break trust: user isolation, timestamps, duplicate prevention, XP consistency, reminders, and finance totals.

V1 does not need enormous test coverage, but it does need tests for high-risk behavior.

## 2. Test rules

- Do not use EF Core InMemory provider for tests that must validate relational constraints, indexes, query filters, or transactions.
- Database-backed tests should use PostgreSQL through a test database, Docker, or Testcontainers when practical.
- Unit tests are acceptable for pure calculation logic.
- Every milestone must run `dotnet build` and `dotnet test` before it is accepted.
- Date/time tests must include at least UTC and Europe/Bucharest.

## 3. Test categories

### 3.1 Unit tests

Use for:

- XP calculation;
- level calculation;
- echelon calculation;
- daily cap logic;
- streak calculation;
- finance summary calculation;
- date/time conversion helpers.

### 3.2 Service tests

Use for:

- task create/edit/complete;
- habit completion;
- XP award idempotency;
- reminder creation;
- notification creation;
- finance transaction workflows.

### 3.3 Integration tests

Use for:

- EF Core constraints;
- migrations;
- user isolation;
- query filters;
- database-backed service flows.

### 3.4 Manual acceptance tests

Use for:

- UI flows;
- dashboard behavior;
- mobile layout;
- reminder visual behavior;
- navigation;
- empty states.

### 3.5 Future AI evaluation

Future AI features need qualitative review plus structured checks for:

- data grounding;
- confidence labels;
- no overclaiming;
- correct time periods;
- privacy boundaries.

## 4. V1 foundation acceptance checklist

- App starts locally.
- Database connection works.
- Migrations apply from a clean database.
- Seed user is created idempotently.
- Development current-user service returns the configured user.
- User settings include time zone.
- Current user service returns correct user.
- Base audit fields are populated.
- Soft delete query filter works where implemented.
- Build passes.
- Tests pass.

## 5. Task acceptance checklist

### 5.1 Task creation

- User can create a task with required title.
- Missing title shows validation error.
- Due date saves correctly.
- Due time saves correctly if provided.
- Due time cannot be saved without a due date.
- Past due dates remain valid and appear as overdue when active.
- Priority saves correctly.
- Category saves correctly.
- Estimated time and friction save correctly.
- Created task appears on list.
- Created task appears on dashboard if due today.

### 5.2 Task editing

- User can edit title, notes, date, priority, category, estimated time, and friction on active tasks.
- Changes persist after refresh.
- User cannot edit another user's task.
- Completed and archived tasks cannot be edited.

### 5.3 Task completion

- User can mark task complete.
- Completed timestamp is stored.
- Completed task leaves active list or moves to completed section.
- Re-clicking complete is a successful no-op and preserves the original completion timestamp and date.

### 5.4 Task deletion/archive

- User can archive or soft-delete a task.
- Archived tasks are available in the archived-task view and do not appear in active views.
- Soft-deleted tasks do not appear in normal task or dashboard queries.
- Archive does not set `IsDeleted`; delete uses EF deletion and `AppDbContext` applies the soft-delete lifecycle.

## 6. Habit acceptance checklist

### 6.1 Habit creation

- User can create a habit.
- Name is required.
- Frequency saves correctly.
- Active status saves correctly.
- Target quantity/unit saves correctly when used.
- Estimated time and friction save correctly.

### 6.2 Habit completion

- User can complete today's habit.
- Habit log stores user, habit, completion date, and completed timestamp.
- Today's habit shows completed immediately.
- Page refresh still shows completed.
- Duplicate completion attempt does not create duplicate log.
- Database unique constraint prevents duplicate log.
- Duplicate completion attempt does not award duplicate XP.

### 6.3 Streaks

- One completion today gives streak of 1 for a new habit.
- Consecutive daily completions increase streak.
- Missing a day breaks basic daily streak.
- Streak calculation uses user's local date.

## 7. XP acceptance checklist

### 7.1 XP calculation

- Under15Minutes low friction gives 50 XP.
- Between15And30Minutes medium friction gives 150 XP.
- Between30And60Minutes high friction gives 300 XP.
- Over60Minutes high friction gives 400 XP.
- Daily quest XP cap prevents excess XP.
- Completing after cap still completes the task/habit but awards no extra quest XP.

### 7.2 XP transaction integrity

- Every XP award creates an XP transaction.
- XP transaction has correct user.
- XP transaction has correct source.
- XP transaction has correct business date.
- Duplicate idempotency key is rejected.

### 7.3 User progression

- Total lifetime XP increases after XP award.
- Level recalculates correctly from the documented level formula.
- Echelon recalculates correctly from documented thresholds.
- Progression update and XP transaction are atomic.
- User progression exists after seeding.

## 8. Reminder and notification acceptance checklist

### 8.1 Reminder creation

- User can create one-time reminder.
- Reminder local time is displayed clearly.
- Reminder UTC value matches local time conversion.
- User time zone is used.
- Invalid reminder time shows validation error.

### 8.2 Reminder firing

- Due reminder creates in-app notification.
- Reminder status changes to fired.
- Fired timestamp is stored.
- Running job twice does not create duplicate notification.
- User cannot see another user's reminder or notification.

### 8.3 Notification behavior

- Notification bell shows unread count.
- User can open notification list.
- User can mark notification as read.
- User can dismiss notification.
- Dismissed notification no longer appears in default unread list.

## 9. Finance acceptance checklist

### 9.1 Transaction creation

- User can create income transaction.
- User can create expense transaction.
- Amount must be positive.
- Date is required.
- Type is required.
- Category saves correctly.
- Description saves correctly.

### 9.2 Monthly summary

- Total income transactions for selected month are correct.
- Total expenses for selected month are correct.
- Remaining planned balance uses planned income/allowance + income transactions - expenses.
- Planned allowance/income is not double-counted.
- Category breakdown is correct.
- Transactions outside selected month are excluded.
- Created date does not affect monthly grouping.

### 9.3 Monthly allowance

- User can set monthly allowance.
- Dashboard displays planned allowance/income and remaining balance.
- Currency displays according to settings.

## 10. Dashboard acceptance checklist

- Dashboard loads after login.
- Dashboard shows today's tasks.
- Dashboard shows overdue tasks.
- Dashboard shows today's habits.
- Dashboard shows habit completion progress.
- Dashboard shows XP level/echelon.
- Dashboard shows unread notifications.
- Dashboard shows simple finance summary.
- Quick-add actions open correct forms.
- Empty states are useful.
- Dashboard does not show broken future widgets.

## 11. Security and privacy acceptance checklist

- `ICurrentUserService` is used to scope every feature workflow; authorization coverage is added with the future Identity milestone.
- User A cannot view User B records through services.
- User A cannot edit User B records by changing IDs.
- Sensitive settings are not committed to repository.
- Logs do not contain passwords or secrets.
- Future AI should not include unrelated user data in context.

## 12. Migration and seed acceptance checklist

- Clean database can be migrated successfully.
- Existing development database can apply latest migration.
- Seed user creation is idempotent.
- Default categories are idempotent.
- User progression seed is idempotent.
- Failed migration logs actionable error.

## 13. Future module test outlines

### 13.1 Sleep and health

- Sleep duration calculates correctly across midnight.
- Sleep date uses user local date.
- Manual and imported records do not conflict unexpectedly.
- Weekly average is correct.

### 13.2 Fitness

- Workout plan can be created.
- Only one active plan per user.
- Session logs sets correctly.
- Total volume calculation is correct.
- Bodyweight exercises allow null weight.
- PR detection works.
- Stall detection triggers only after defined threshold.

### 13.3 Body metrics

- Weight trend uses correct dates.
- Measurement deltas are correct.
- Phase tag applies to correct period.
- Progress photo metadata is stored without exposing private paths.

### 13.4 Nutrition

- Meal macros sum correctly by day.
- Protein target progress is correct.
- Meal template quick-add creates expected meal entry.
- Meal prep planned versus actual is correct.

### 13.5 Study/projects

- Study target hours calculate by week.
- Project lifetime hours are correct.
- Pomodoro completion logs a session.
- Manual override logs a session.
- Neglected subject detection uses correct threshold.

### 13.6 Wellbeing/journal

- One daily wellbeing record per user/date.
- Mood, energy, stress must be 1-5.
- Journal text is private and user-scoped.
- Weekly intention is unique per user/week.

### 13.7 AI

- AI uses only current user's data.
- AI states time period used.
- AI identifies low sample size.
- AI avoids medical/financial certainty.
- AI explains the basis for recommendations.

### 13.8 Advanced finance

- Import preview does not create transactions until confirmed.
- Duplicate detection prevents repeated imports.
- Budget alert thresholds trigger correctly.
- Subscription renewal dates display correctly.
- Savings projections calculate correctly.

## 14. Regression tests from prototype lessons

These tests specifically guard against known prototype issues:

- HabitLog duplicate insertion fails at database level.
- DailyScore duplicate insertion fails at database level when DailyScore is implemented later.
- Reminder local form value is converted to UTC, not specified as UTC.
- Protected pages require authorization.
- UserProgression is seeded before XP display loads.
- XP service failures do not crash the Blazor circuit without a user-visible message.
- Razor UI does not bypass services for core workflows.
- Soft-deleted tasks do not appear in active dashboard.
- Date-only comparisons are not made by calling `.Date` on arbitrary local/UTC timestamps where a date-only model should exist.

## 15. Release acceptance

V1 can be considered ready for personal use when:

- all V1 milestone checklists pass;
- one clean database install works;
- one realistic day of manual use works;
- no duplicate XP or habit logs appear;
- no DailyScore placeholder creates misleading module zeros;
- reminder timing is verified in the configured time zone;
- finance monthly summary matches hand calculation;
- docs match implemented behavior.
