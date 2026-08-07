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
- monthly allowance;
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
- archive or soft-delete habit;
- active/inactive status;
- daily frequency only;
- binary completion;
- optional measurable target field;
- completion for today;
- habit completion log;
- duplicate completion prevention;
- basic streak display;
- completion history list or simple calendar.

V1 does not include selected-day habits, weekly/monthly habit views, multiple completions per day, momentum streaks, or weekly streaks.

### 5.4 Reminders and notifications

V1 includes:

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

V1 includes:

- XP transaction log;
- user progression record;
- quest XP from task and habit completion;
- XP based on estimated time and friction;
- daily quest XP cap;
- total lifetime XP;
- level calculation;
- echelon calculation;
- basic level/echelon display;
- level-up notification.

V1 does not include the full DailyScore engine. DailyScore is a future feature because it needs more than tasks and habits to avoid misleading scoring.

### 5.6 Dashboard

V1 includes:

- today tasks;
- due/overdue tasks;
- today habits;
- habit completion progress;
- XP/level summary;
- active notifications/reminders;
- simple finance snapshot;
- quick-add actions.

V1 dashboard should not contain empty future widgets for modules that do not exist yet.

### 5.7 Simple finance

V1 finance is intentionally simple.

V1 includes:

- manual transaction entry;
- income and expense transaction types;
- monthly allowance or planned monthly income configuration;
- manual income entries for extra income/refunds/gifts;
- categories;
- transaction date;
- amount;
- description/notes;
- monthly total income;
- monthly total expenses;
- remaining monthly balance using the formula defined in the SRS;
- spending by category;
- simple finance dashboard card.

V1 does not include:

- Revolut import;
- Raiffeisen import;
- CSV/XLS parsing;
- AI categorization;
- merchant normalization;
- subscriptions;
- savings projections;
- budget alerts;
- net worth snapshots;
- financial advice.

### 5.8 Settings

V1 includes:

- user profile basics;
- time zone setting;
- theme preference if practical;
- finance default currency;
- monthly allowance configuration;
- XP display preferences if needed.


### 5.9 Non-negotiable V1 product decisions

These decisions remove ambiguity before implementation:

- Every personal record uses a stable Guid `UserId` from the start through `ICurrentUserService`; future Identity integration must provide this abstraction.
- V1 habits are daily-only. More complex schedules are deferred.
- V1 task due dates are calendar dates, with optional local due time for planning. Reminder delivery is handled by the Reminder module, not by task due-time shortcuts.
- V1 quest XP uses the documented Time Base times Friction Multiplier formula.
- V1 does not implement the full DailyScore engine or streak bonus XP job.
- V1 finance uses one monthly plan amount for expected money available, plus manual transactions for actual expenses and optional extra income.
- V1 reminders are one-time in-app reminders only.
- V1 UI pages use services for feature workflows.

## 6. V1 out of scope

The following modules remain part of the product vision but are out of scope for V1 implementation:

- Sleep tracking;
- health markers;
- selected-day, weekly, and monthly habit frequencies;
- advanced daily score across all life domains;
- workout plan builder;
- workout session logger;
- progressive overload tracker;
- body metrics and progress photos;
- nutrition logging;
- meal prep planner;
- study tracker;
- project tracker;
- Pomodoro/focus timer;
- wellbeing daily check-in;
- journal;
- weekly intentions;
- AI assistant;
- weekly AI review;
- cross-domain correlation engine;
- Garmin import;
- finance imports;
- subscriptions;
- savings goals;
- net worth;
- browser push notifications;
- offline-first support;
- public multi-user/social features.

Out of scope does not mean deleted. These modules are preserved in the roadmap and backlog.

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

As the user, I want to manually log income and expenses so that I know how much of my monthly allowance remains.

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

## 9. Version roadmap themes

Separate V2 and V3 PRDs should not be fully locked before V1 is used. However, the following release themes preserve the long-term direction.

### 9.1 V1.1 - Lightweight life logs

Candidate modules:

- daily wellbeing check-in;
- basic sleep log;
- simple journal;
- weekly intentions.

Purpose:

- add subjective and recovery data;
- improve future daily score;
- prepare data for AI reviews.

### 9.2 V2 - Training and physique foundation

Candidate modules:

- workout plan builder;
- session logger;
- progressive overload tracker;
- exercise library;
- body metrics;
- phase tagging.

Purpose:

- support the user's gym and aesthetic physique goals;
- create high-value structured performance data.

### 9.3 V2.5 - Nutrition and meal prep

Candidate modules:

- meal logging;
- estimated macros;
- protein/calorie/water targets;
- meal templates;
- meal prep planner.

Purpose:

- connect nutrition to fitness and body metrics;
- keep tracking practical without building a full food database.

### 9.4 V3 - Study, projects, and focus

Candidate modules:

- study subjects;
- weekly targets;
- study sessions;
- personal projects;
- project work sessions;
- Pomodoro/focus timer;
- neglect detection.

Purpose:

- support university work, internships, and personal project consistency.

### 9.5 V4 - AI assistant and cross-domain insight

Candidate modules:

- AI chat;
- weekly review;
- data-aware recommendations;
- confidence-aware insights;
- finance summaries;
- physique reports;
- study/project summaries;
- correlation engine.

Purpose:

- turn accumulated structured data into useful reflection and recommendations.

### 9.6 Later - Advanced integrations

Candidate modules:

- finance imports;
- Garmin import;
- export/reporting;
- offline capture;
- multi-user support;
- external notification channels.

## 10. Product decision log

### Decision 1 - Rebuild from scratch

The prototype was an experiment. The rebuild starts from clean documentation and clean architecture.

### Decision 2 - Finance simplified in V1

V1 finance is manual tracking only. Complex finance features are future backlog items.

### Decision 3 - AI delayed

AI is not in V1 because the app needs clean data first.

### Decision 4 - Future modules preserved

Sleep, health, fitness, nutrition, body metrics, study, projects, wellbeing, AI, Garmin, and advanced finance remain part of the official product vision and backlog.

### Decision 5 - Daily habits only in V1

Selected-day, weekly, monthly, and multi-completion habits are valuable, but they are deferred so the first habit system can be made duplicate-safe and testable.

### Decision 6 - No full DailyScore in V1

V1 uses quest XP and progression only. DailyScore becomes useful after additional modules such as wellbeing, sleep, finance, fitness, and nutrition exist.

### Decision 7 - Finance formula clarified

The monthly plan defines expected money available for the month. Manual transactions record expenses and optional extra income. The app must avoid double-counting the monthly allowance as both planned income and a separate income transaction.
