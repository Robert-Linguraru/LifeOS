# 01 - V1 Scope PRD

## 1. Purpose

This document defines the product scope for LifeOS V1. It intentionally separates the first shippable version from the full product vision.

The original product vision includes many modules: productivity, habits, finance, health, fitness, nutrition, study, projects, wellbeing, AI, and future device integrations. V1 should not attempt to build all of them at once.

V1 must instead prove that the LifeOS foundation is reliable, extensible, and useful every day.

## 2. V1 product goal

V1 should become a stable daily operating system for:

- planning tasks;
- reinforcing habits;
- receiving simple reminders;
- seeing today's priorities;
- earning XP from meaningful actions;
- tracking simple personal finance manually;
- building a clean foundation for later modules.

V1 is not the full LifeOS. It is the launchpad.

## 3. V1 positioning

LifeOS V1 is a private daily command center with gamified execution.

The user should open it in the morning to see what matters, use it during the day to check off tasks and habits, and use it at night to review progress and add simple finance entries.

## 4. V1 target user

The V1 user is the same primary user from the vision document:

- student;
- training-focused;
- no current salary;
- personal finance tracking;
- building a productivity system from scratch;
- comfortable with a structured app;
- motivated by progression systems.

V1 should be optimized for one real user before it tries to support broader public use.

## 5. V1 in scope

### 5.1 Foundation

V1 includes:

- Blazor Server web app;
- .NET solution structure;
- PostgreSQL database;
- EF Core migrations;
- development current-user support through `ICurrentUserService`;
- user-owned entities;
- base entity audit fields;
- service layer;
- global navigation;
- dark JARVIS-inspired theme;
- responsive layout basics;
- PWA manifest shell.

### 5.2 Tasks

V1 includes:

- create task;
- edit task;
- delete or soft-delete task;
- complete task;
- due date;
- optional due time;
- priority;
- category/domain;
- notes;
- estimated time;
- friction level;
- status: `TaskItemStatus` active, completed, or archived; archive and soft delete are distinct operations;
- today view;
- overdue handling;
- simple list filters.

V1 does not include recurring tasks, snooze, or advanced time blocking.

### 5.3 Habits

V1 includes:

- create habit;
- edit habit;
- archive habit; new habits begin active and archived habits are read-only;
- daily frequency only;
- binary completion event;
- optional quantity-target metadata without quantity entry during completion;
- completion for today in the user's local time zone;
- immutable habit completion log;
- idempotent duplicate completion with a unique user/habit/date constraint;
- basic current streak with defined consecutive-local-date semantics;
- simple newest-first completion history list for active and archived habits.

V1 does not include habit restore/reactivate, user-facing habit deletion or soft deletion, selected-day habits, weekly/monthly habit views, multiple completions per day, quantity achievement entry, momentum streaks, or weekly streaks.

### 5.4 Reminders and notifications

Milestone 6 includes:

- one-time reminder attached to a task or habit;
- reminder date/time entry;
- correct local-time to UTC conversion;
- background job checks for due reminders;
- in-app notification creation;
- notification bell/list;
- mark as read/dismiss;
- idempotent processing so reminders do not fire twice.

V1 does not include browser push, email, mobile push, recurring reminders, snooze, or full reminder history.

### 5.5 Gamification core

Milestone 5 includes:

- XP transaction log;
- user progression record;
- quest XP from task and habit completion;
- XP based on estimated time and friction;
- daily quest XP cap;
- total lifetime XP;
- level calculation;
- echelon calculation;
- basic level/echelon display;
- level/echelon transition feedback (persisted notifications are Milestone 6).

V1 does not include the full DailyScore engine. DailyScore is a future feature because it needs more than tasks and habits to avoid misleading scoring.

### 5.6 Dashboard

V1 dashboard capabilities arrive with their owning milestones:

- Milestone 3: today's and due/overdue tasks;
- Milestone 4: today's Habits and Habit completion progress;
- Milestone 5: XP/level summary;
- Milestone 6: active notifications/reminders;
- simple finance snapshot;
- quick-add actions.

V1 dashboard should not contain empty future widgets for modules that do not exist yet.

### 5.7 Simple finance

V1 finance is intentionally simple.

V1 includes:

- manual transaction entry;
- income and expense transaction types;
- manual income and expense entries;
- categories;
- transaction date;
- positive amount;
- optional description;
- one user-level finance currency preference;
- monthly total income;
- monthly total expenses;
- monthly net cash flow (`Income - Expenses`);
- spending by category;
- selected-year total income, total expenses, and net cash flow;
- simple finance dashboard card.

V1 does not include:

- Revolut import;
- Raiffeisen import;
- CSV/XLS parsing;
- AI categorization;
- merchant normalization;
- category budgets;
- subscriptions;
- savings goals;
- projections and forecasting;
- budget alerts;
- net worth snapshots;
- financial advice.

### 5.8 Settings

V1 includes:

- user profile basics;
- time zone setting;
- theme preference if practical;
- finance currency preference;
- XP display preferences if needed.


### 5.9 Non-negotiable V1 product decisions

These decisions remove ambiguity before implementation:

- Every personal record uses a stable Guid `UserId` from the start through `ICurrentUserService`; future Identity integration must provide this abstraction.
- V1 habits are daily-only. More complex schedules are deferred.
- V1 task due dates are calendar dates, with optional local due time for planning. Reminder delivery is handled by the Reminder module, not by task due-time shortcuts.
- V1 quest XP uses the documented Time Base times Friction Multiplier formula.
- V1 does not implement the full DailyScore engine or streak bonus XP job.
- M7 Simple Finance uses only manual income and expense transactions. Net cash flow is income minus expenses; there is no planned-income or allowance source.
- V1 reminders are one-time in-app reminders only.
- V1 UI pages use services for feature workflows.

## 6. Deferred from the approved operational V1 roadmap

In this document, the completed M0-M7 baseline and the approved M8-M13 roadmap together define the operational V1 product. Fitness, Nutrition, Health, Calendar, and the M13 UI/UX overhaul are planned V1 work, not out of scope. The following remain deferred beyond that roadmap:

- selected-day, weekly, and monthly habit frequencies;
- advanced daily score across all life domains;
- multi-week workout programming, periodization, automated progression, RPE/RIR, and custom exercises;
- body metrics and progress photos;
- meal prep planner;
- study tracker;
- project tracker;
- Pomodoro/focus timer;
- journal;
- weekly intentions;
- AI assistant;
- weekly AI review;
- cross-domain correlation engine;
- Garmin, Strava, wearable, and other device imports;
- finance imports;
- subscriptions;
- savings goals;
- net worth;
- browser push notifications;
- offline-first support;
- public multi-user/social features.

Deferred does not mean deleted. These modules are preserved as future work in the canonical roadmap and backlog.

## 7. V1 user stories

### 7.1 Morning planning

As the user, I want to open the dashboard and see today's tasks, habits, reminders, and progress so that I know what to focus on.

### 7.2 Quick task capture

As the user, I want to add a task quickly with a due date, priority, and notes so that I do not lose responsibilities.

### 7.3 Task completion

As the user, I want to complete a task and receive XP when appropriate so that execution feels rewarding.

### 7.4 Habit consistency

As the user, I want to check off today's habits so that I can build consistency and see streak progress.

### 7.5 Reminder dependability

As the user, I want reminders to appear at the time I selected so that I can trust the app.

### 7.6 Finance awareness

As the user, I want to manually log income and expenses so that I can see my monthly and yearly net cash flow.

### 7.7 Progression motivation

As the user, I want to see my level, XP, and echelon so that my daily effort feels cumulative.

## 8. V1 success criteria

V1 is successful if:

- the app can be used daily for tasks and habits;
- completing tasks and habits never creates duplicate XP transactions;
- habit logs cannot be duplicated for the same habit/date/user;
- reminders fire at the intended local time;
- finance totals are correct for the selected month;
- the dashboard loads quickly and is useful;
- all feature workflows are user-scoped through `ICurrentUserService`;
- all user-owned data is user-scoped;
- migrations work from a clean database;
- future modules can be added without major restructuring.

## 9. Approved post-M7 roadmap

M0–M7 are complete. The approved remaining sequence is:

1. M8 — Strength & Calisthenics Training;
2. M9 — Activity & Fitness Progression;
3. M10 — Nutrition;
4. M11 — Health;
5. M12 — Calendar;
6. M13 — V1 UI/UX Overhaul & Polish;
7. M14 — Analytics.

`16-post-m7-domain-roadmap.md` is the canonical requirements source for these planned milestones. The product principle is to capture high-quality structured data and implement useful deterministic domain capabilities before deeper analytics or AI. AI is future work after Analytics and has no assigned milestone number.

## 10. Product decision log

### Decision 1 - Rebuild from scratch

The prototype was an experiment. The rebuild starts from clean documentation and clean architecture.

### Decision 2 - Finance simplified in V1

V1 finance is manual tracking only. Complex finance features are future backlog items.

### Decision 3 - AI delayed

AI is not in V1 because the app needs clean data first.

### Decision 4 - Planned and future modules preserved

Fitness is approved for M8-M9, Nutrition for M10, Health for M11 including body weight, sleep, and structured wellbeing, Calendar for M12, UI/UX overhaul and polish for M13, and Analytics for M14. Broader body metrics, study, projects, journals, AI, Garmin/device integrations, advanced Finance, and other deferred ideas remain future work in the official product vision and backlog.

### Decision 5 - Daily habits only in V1

Selected-day, weekly, monthly, and multi-completion habits are valuable, but they are deferred so the first habit system can be made duplicate-safe and testable.

### Decision 6 - No full DailyScore in V1

V1 uses quest XP and progression only. DailyScore becomes useful after additional modules such as wellbeing, sleep, finance, fitness, and nutrition exist.

Milestone 6 is the canonical Notifications and One-Time Reminders increment. It
includes Task, Habit, and Custom reminders, explicit timezone confirmation,
in-app notifications, the notification bell, Reminders/Notifications pages, and
the widget-specific Dashboard Reminder projection. Recurrence, snooze, Quick Add,
external delivery, and source-driven cancellation remain out of scope.

### Decision 7 - M7 Simple Finance contract

M7 is a lightweight manual personal income-and-expense tracker. Transactions
have a positive amount and a type; `Income - Expenses` is the authoritative
monthly and yearly calculation. There is no `MonthlyFinancePlan`, planned
income, allowance, expense target, import source, or per-transaction currency.
The configured user-level finance currency is a display/base convention only;
changing it does not convert historical amounts.
