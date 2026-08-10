# 09 - Backlog Document

## 1. Purpose

This backlog preserves the full LifeOS product vision while keeping V1 manageable.

Items in this backlog are not deleted. They are deferred until their prerequisite systems are stable.

## 2. Status labels

- `V1` - required for first rebuild.
- `V1.1` - likely next after V1.
- `V2` - major post-V1 module.
- `V3` - later expansion.
- `V4` - AI and advanced insight layer.
- `Future` - preserved, not scheduled yet.

## 3. V1 backlog

### Foundation

- `V1` Project scaffold.
- `V1` App shell.
- `V1` Dark JARVIS-inspired theme.
- `V1` Navigation layout.
- `V1` PostgreSQL setup.
- `V1` EF Core migrations.
- `Current` DevelopmentCurrentUserService.
- `Future` ASP.NET Identity implementation behind ICurrentUserService.
- `Future` Protected pages with Identity authorization.
- `V1` User settings with time zone and currency.
- `V1` BaseEntity.
- `V1` Current user service.
- `V1` Date/time provider.
- `V1` Soft delete policy.
- `V1` Service layer pattern.

### Tasks

- `V1` Task CRUD.
- `V1` Due date and optional due time.
- `V1` Priority.
- `V1` Category/domain.
- `V1` Notes.
- `V1` Estimated time.
- `V1` Friction level.
- `V1` Completion.
- `V1` Today and overdue views.
- `V1` Dashboard task widget.

### Habits

- `Milestone 4` Habit create, read, update, and archive workflows.
- `Milestone 4` Active/archive lifecycle using `IsActive`; no restore/reactivate or user-facing delete.
- `Milestone 4` Daily habits only.
- `Milestone 4` Binary completion with optional quantity-target metadata and no achieved-quantity entry.
- `Milestone 4` Immutable HabitLog.
- `Milestone 4` Unique `(UserId, HabitId, CompletionDate)` constraint with idempotent duplicate completion.
- `Milestone 4` Basic current daily streak and newest-first completion history.
- `Milestone 4` Dashboard Habit widget with completion progress.
- `Milestone 5` Habit completion XP integration.
- `Milestone 6` Habit reminder integration.

### XP and gamification

- `V1` XPTransaction.
- `V1` UserProgression.
- `Milestone 5` Quest XP.
- `Milestone 5` Daily quest XP cap.
- `Milestone 5` Level calculation.
- `Milestone 5` Echelon calculation.
- `Milestone 5` XP dashboard display.
- `Milestone 5` Basic level-up notification.
- `Future` DailyScore engine.
- `Future` Streak bonus XP job.

### Reminders and notifications

- `Milestone 6` Reminder entity.
- `Milestone 6` Notification entity.
- `Milestone 6` One-time reminders.
- `Milestone 6` Local-time to UTC conversion.
- `Milestone 6` Due reminder background job.
- `Milestone 6` In-app notification bell.
- `Milestone 6` Mark read/dismiss.

### Simple finance

- `V1` Manual income transaction.
- `V1` Manual expense transaction.
- `V1` Default categories.
- `V1` Monthly planned income/allowance.
- `V1` Monthly income total.
- `V1` Monthly expense total.
- `V1` Remaining planned balance.
- `V1` Spending by category.
- `V1` Finance dashboard card.

### V1 polish

- `V1` Empty states.
- `V1` Loading states.
- `V1` Validation.
- `V1` Responsive layout.
- `V1` PWA manifest.
- `V1` Error handling.
- `V1` Clean install verification.

## 4. V1.1 backlog - lightweight life logs

### Habit schedule expansion

- `V1.1` Selected-day habits.
- `V1.1` Weekly habit schedules if the model is clear.
- `Future` Monthly habit schedules.

### Daily wellbeing

- `V1.1` Daily mood score 1-5.
- `V1.1` Daily energy score 1-5.
- `V1.1` Daily stress score 1-5.
- `V1.1` One record per user/date.
- `V1.1` Dashboard check-in card.

### Journal

- `V1.1` Free-form daily journal.
- `V1.1` Optional guided prompts.
- `V1.1` Quick field: what drained me today.
- `Future` Searchable journal history.
- `Future` AI weekly review context with opt-in.

### Weekly intentions

- `V1.1` Monday intention entry with three priorities.
- `V1.1` Dashboard intention display.
- `Future` Sunday AI review of intention alignment.

### Basic sleep

- `V1.1` Sleep date.
- `V1.1` Bedtime.
- `V1.1` Wake time.
- `V1.1` Sleep duration.
- `V1.1` Sleep quality.
- `V1.1` Weekly average.

### Daily score foundation

- `V1.1` DailyScore using only configured modules.
- `V1.1` Excluded-denominator scoring model.
- `Future` DailyScore XP job after scoring is trusted.

## 5. V2 backlog - fitness and progressive overload

### Exercise library

- `V2` Preloaded exercises.
- `V2` Custom exercises.
- `V2` Primary muscle group.
- `V2` Secondary muscles.
- `V2` Equipment type.

### Workout plan builder

- `V2` Named workout plans.
- `V2` Active/inactive plans.
- `V2` One active plan per user.
- `V2` Training day labels: Push, Pull, Legs, Upper, Lower, Rest, Custom.
- `V2` Planned exercises with target sets, reps, and weights.

### Session logging

- `V2` Start session from dashboard.
- `V2` Load planned exercises.
- `V2` Log sets.
- `V2` Reps completed.
- `V2` Weight used.
- `V2` Completed/failed/skipped status.
- `V2` Rest timer.
- `V2` Session notes.
- `V2` Session duration.
- `V2` Total volume.

### Home workout support

- `V2` Bodyweight exercises.
- `V2` Home workout templates.
- `V2` Bodyweight sessions without weight fields.
- `V2` Home workouts count toward daily score once daily score expands.

### Progressive overload

- `V2` Lift history per exercise.
- `V2` Weight trend chart.
- `V2` Volume trend chart.
- `V2` PR detection.
- `V2` Stall detection after repeated non-progression.
- `Future` AI deload or volume suggestions.

## 6. V2.5 backlog - body metrics and nutrition

### Body metrics

- `V2.5` Body weight log.
- `V2.5` Chest measurement.
- `V2.5` Waist measurement.
- `V2.5` Hips measurement.
- `V2.5` Arm measurements.
- `V2.5` Thigh measurements.
- `V2.5` Shoulder measurement.
- `V2.5` Optional body fat percentage.
- `V2.5` Phase tagging: bulk, cut, maintain.
- `V2.5` Weight trend chart.
- `V2.5` Measurement delta view.
- `V2.5` Rate of change calculation.
- `Future` Progress photo storage.
- `Future` AI physique progress report.

### Nutrition

- `V2.5` Meal logging.
- `V2.5` Estimated protein.
- `V2.5` Estimated carbs.
- `V2.5` Estimated fat.
- `V2.5` Estimated calories.
- `V2.5` Water intake.
- `V2.5` Daily protein target.
- `V2.5` Daily calorie target.
- `V2.5` Daily water target.
- `V2.5` Meal templates.
- `V2.5` Supplement habits as quests.
- `V2.5` Nutrition dashboard.

### Meal prep

- `V2.5` Weekly meal prep plan.
- `V2.5` Planned meals.
- `V2.5` Portions.
- `V2.5` Target days.
- `V2.5` Planned versus actual meal view.
- `V2.5` Meal prep session as quest.

## 7. V3 backlog - study, projects, and focus

### Study tracker

- `V3` Study subjects.
- `V3` Weekly target per subject.
- `V3` Study session logging.
- `V3` Topic covered.
- `V3` Method: Pomodoro, deep work, review, lecture, other.
- `V3` Study consistency dashboard.
- `Future` AI neglected subject alerts.

### Project tracker

- `V3` Personal project definition.
- `V3` Project status: Idea, In Progress, Paused, Shipped.
- `V3` Tech stack field.
- `V3` GitHub link.
- `V3` Project work sessions.
- `V3` Lifetime hours per project.
- `V3` Project timeline.
- `Future` Portfolio/internship summary.

### Focus sessions

- `V3` Pomodoro timer.
- `V3` Configurable focus block.
- `V3` Short break.
- `V3` Long break.
- `V3` Auto-log completed focus sessions.
- `V3` Manual override.
- `Future` Calendar/time-block view.

## 8. V4 backlog - AI and insights

### AI assistant

- `V4` AI chat UI.
- `V4` Approved data access tools.
- `V4` Chat history.
- `V4` Local model provider abstraction.
- `V4` Prompt templates.
- `V4` Confidence-aware answers.
- `V4` Explain data basis.

### Weekly review

- `V4` Weekly summary job.
- `V4` Wins and misses.
- `V4` XP sources.
- `V4` Habit consistency.
- `V4` Task completion.
- `V4` Finance summary.
- `V4` Sleep/wellbeing summary once available.
- `V4` Intention alignment once available.

### Cross-domain insights

- `V4` Sleep versus productivity.
- `V4` Sleep versus workout performance.
- `V4` Spending versus stress.
- `V4` Nutrition versus training.
- `V4` Study consistency versus mood/energy.
- `V4` Confidence labels based on sample size.

### AI reports

- `V4` Finance summary.
- `V4` Physique progress report.
- `V4` Study/project progress report.
- `V4` Monthly life review.

## 9. Future backlog - advanced finance

Advanced finance is preserved but not V1.

- `Future` Revolut CSV parser.
- `Future` Raiffeisen CSV parser.
- `Future` Raiffeisen XLS parser.
- `Future` Import batch entity.
- `Future` Import preview.
- `Future` Duplicate detection.
- `Future` Merchant normalization.
- `Future` AI category suggestions.
- `Future` Monthly budgets by category.
- `Future` Budget progress bars.
- `Future` Alerts at 80 percent and 100 percent.
- `Future` Subscription manager.
- `Future` Worth-it flag for subscriptions.
- `Future` Savings goals.
- `Future` Required savings rate calculation.
- `Future` Net worth snapshots.
- `Future` Finance AI summary.

## 10. Future backlog - Garmin and imports

- `Future` Garmin Connect sleep CSV import.
- `Future` Garmin workout import.
- `Future` HRV import.
- `Future` Recovery score import.
- `Future` Body metrics sync where available.
- `Future` Manual/imported data conflict handling.
- `Future` CSV export.
- `Future` JSON export.
- `Future` PDF report export.

## 11. Future backlog - achievements and advanced gamification

- `Future` Daily score full module weighting.
- `Future` Streak bonus XP job.
- `Future` Momentum streaks.
- `Future` Weekly streaks.
- `Future` Achievement badges.
- `Future` Milestone celebrations.
- `Future` Streak recovery mechanics.
- `Future` Echelon border animations.

## 12. Future backlog - PWA and offline

- `Future` Browser push notifications.
- `Future` Offline quick capture.
- `Future` Queued mobile actions.
- `Future` Better install flow.
- `Future` Tailscale deployment notes if personally hosted.

## 13. Future backlog - multi-user

- `Future` User onboarding.
- `Future` User profile management.
- `Future` Stronger data isolation tests.
- `Future` Per-user AI context boundaries.
- `Future` Admin/debug views.
- `Future` Private sharing if ever desired.

## 14. Backlog prioritization rules

Prioritize a backlog item when it:

- improves daily use;
- depends on stable data already available;
- adds high-value insight;
- follows existing patterns;
- does not require risky architecture changes.

Defer a backlog item when it:

- adds complex infrastructure before user value;
- requires data that does not exist yet;
- duplicates an external app without LifeOS-specific value;
- risks breaking the foundation;
- makes the app feel like a data-entry burden.

## 15. Current recommendation

After V1, the best next module is likely either:

1. Wellbeing plus basic sleep, because it is simple and useful for future AI.
2. Fitness/progressive overload, because it is highly aligned with the user's goals.

Do not decide this permanently before V1 is actually used.
