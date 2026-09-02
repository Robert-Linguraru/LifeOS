# 00 - Product Vision Document

## 1. Purpose

This document defines the long-term product vision for LifeOS. It is intentionally broader than V1. It preserves all major modules from the original planning draft while making clear that not every module belongs in the first build.

The purpose of this document is to answer:

- What is LifeOS?
- Who is it for?
- What should it eventually become?
- Which modules exist in the product universe?
- Which principles should guide every version?

The purpose of this document is not to define the exact V1 build. V1 is defined separately in `01-v1-scope-prd.md`.

## 2. Product summary

LifeOS is a personal life management web application designed to centralize daily responsibilities, personal metrics, routines, training, study, finance, wellbeing, and AI-assisted reflection.

The long-term product is a private personal operating system: a place where the user can plan the day, track execution, review trends, and receive useful feedback from their own data.

The application begins as a single-user product, but it must be structured so multi-user support can be added later without destructive database rework.

## 3. Long-term vision

LifeOS should become a JARVIS-style personal command center:

- dark, sleek, technical, and motivating;
- fast enough for daily use;
- broad enough to connect multiple life domains;
- intelligent enough to turn raw logs into explanations and suggestions;
- safe enough that the user always remains in control.

The user should eventually be able to ask questions such as:

- Why did my discipline drop this week?
- Did poor sleep affect my workout performance?
- Am I overspending because of stress or social events?
- Which study subjects have I neglected?
- Is my bulk, cut, or maintenance phase progressing as expected?
- Where did most of my XP come from this month?
- What should I focus on next week?

LifeOS should not become a noisy data-entry burden. The central design challenge is to collect enough structured information to be useful without making the user feel punished by the system.

## 4. Target user

The primary user is:

- a Computer Science and Economics student;
- managing personal finances alongside study, training, and other responsibilities;
- training at the gym and pursuing an aesthetic physique;
- technically capable and comfortable with structured tracking;
- building a productivity system from scratch;
- interested in internships, projects, and study consistency;
- open to AI suggestions but unwilling to give up control.

Future users may include friends or private early users with similar needs. Public social features are not part of the early vision.

## 5. Product principles

### 5.1 Speed first

Common actions should be fast: complete a task, log a habit, add an expense, log mood, or record a workout should take as few taps or clicks as possible.

### 5.2 Clarity over decoration

The interface may be cinematic and JARVIS-inspired, but usability wins over visual effects.

### 5.3 Progress over perfection

Missing a day should not make the app feel punitive. The system should encourage recovery and momentum.

### 5.4 Cross-domain intelligence

The long-term advantage of LifeOS is not that it tracks many things. It is that it can connect them. Sleep, workouts, food, spending, mood, study, and habits should eventually inform each other.

### 5.5 Human-in-the-loop AI

AI should summarize, question, explain, and suggest. It should not silently take action or make strong medical, financial, or life decisions for the user.

### 5.6 Privacy by default

LifeOS contains sensitive personal data. User boundaries, local-first AI options, minimal data sharing, and clear consent should be treated as product fundamentals.

### 5.7 Extensible modules

The app should start small, but the architecture should allow future modules to be added without rewriting the foundation.

## 6. Complete product module map

This section preserves the complete module universe from the original planning work.

### 6.1 Foundation and identity

The foundation module supports:

- app shell;
- navigation;
- authentication;
- seeded single-user setup;
- future multi-user readiness;
- protected routes;
- user-scoped services;
- persistent settings;
- theme and layout system;
- database migrations and seed workflow.

### 6.2 Tasks

Tasks support one-off responsibilities and later recurring responsibilities.

Long-term task capabilities include:

- create one-time tasks;
- set due date and optional due time;
- set priority, category, tags, notes, estimated time, and friction;
- complete, edit, delete, snooze, and reschedule;
- support today view, backlog view, overdue view, and filtered views;
- later support recurring tasks and generated occurrences;
- award quest XP through controlled services.

V1 includes one-time tasks and basic completion. Recurring tasks, snooze, and advanced filters can wait until the base task system is reliable.

### 6.3 Habits

Habits support recurring behaviors and consistency tracking.

Long-term habit capabilities include:

- binary habits;
- measurable habits;
- daily, selected-day, weekly, and monthly frequencies;
- scheduled days;
- target quantities;
- completion logs;
- completion rate;
- daily streaks;
- momentum streaks;
- weekly streaks;
- missed-day reason or friction tracking;
- charts and calendar views;
- XP through quest completion and streak bonuses.

Milestone 4 includes daily Habit creation, editing, archive lifecycle, binary completion logging, optional quantity-target metadata, basic current streak, newest-first history, and idempotent duplicate prevention. Selected-day, weekly, monthly, momentum, multi-completion, quantity-achievement entry, and restore/reactivate behavior are preserved as future scope. Habit XP integration belongs to Milestone 5.

### 6.4 Reminders and notifications

The reminder system supports time-sensitive follow-through.

Long-term capabilities include:

- in-app notifications;
- one-time reminders;
- recurring reminders;
- snooze;
- dismiss/read states;
- reminder delivery history;
- browser push notifications;
- email or external channels later;
- level-up and echelon-change notifications;
- background scheduling via Hangfire with PostgreSQL storage, one due-reminder
  invocation per minute, batches of at most 100, and three automatic retries.

V1 includes in-app notifications and one-time reminders only. Milestone 6 freezes
the Reminder/Notification persistence, timezone, DST, idempotency, worker, XP
transition, Dashboard bell/widget, and bUnit/manual verification contracts in
the Milestone 6 build plan. Browser push and recurring reminders are future
features.

### 6.5 Dashboard

The dashboard is the central command center.

Long-term dashboard widgets include:

- today tasks;
- today habits;
- active reminders;
- XP and level progression;
- daily score;
- active streaks;
- finance snapshot;
- sleep summary;
- workout prompt;
- nutrition summary;
- wellbeing check-in;
- study/project focus;
- AI insight panel;
- weekly intention panel;
- quick-add actions.

V1 dashboard should be simple and reliable. It should not try to display every future module before data exists.

### 6.6 Gamification

The gamification system turns normal app usage into measurable personal progression.

Long-term system elements include:

- XP transactions as the audit log;
- user progression record;
- quest XP from task and habit completion;
- daily quest XP cap;
- daily life score;
- streak bonus XP;
- daily, weekly, and momentum streaks;
- levels;
- echelons with cosmetic profile borders;
- level/echelon transition feedback;
- weekly XP summaries;
- AI explanations of XP sources.

Milestone 5 includes the append-only XP ledger, user progression, task/habit Quest XP, a shared 500-XP daily cap, exact levels and echelons, and transition metadata. It does not create persisted notifications or add a global/header XP chip. Notification persistence belongs to Milestone 6. The full DailyScore engine should wait until more life-domain modules exist; V1 should not create misleading zero-based daily scores for modules that are not implemented.

### 6.7 Health

M11 Health is deliberately manual, lightweight, and non-medical. It owns authoritative body weight, sleep, and daily wellbeing. Sleep captures bedtime, wake time, calculated duration, and optional categorical quality. The rapid wellbeing check-in captures categorical Energy, Mood, Stress, and Overall values as defined in the canonical roadmap. Medical data, caffeine/symptom markers, readiness scoring, device-derived health data, and trend/correlation analytics remain future work.

### 6.8 Fitness

Fitness is a high-priority planned domain, beginning with M8 Strength & Calisthenics Training and broadening in M9 Activity & Fitness Progression.

Long-term capabilities include:

- one shared gym/strength and calisthenics Exercise Library;
- reusable workout templates rather than multi-week programs;
- mode-appropriate set logging and persisted active sessions;
- session logging;
- set-level logging;
- gym and calisthenics support through the same domain architecture;
- bodyweight exercise support;
- rest timer;
- notes;
- total volume calculation;
- personal record detection;
- exercise history, derived PRs, and performance Fitness Goals;
- M9 running, walking, steps, sports, and other activity.

Templates are recipes and completed sessions are historical truth. Programming, RPE/RIR, custom exercises, device data, advanced analytics, and AI coaching are deferred.

### 6.9 Body metrics and physique tracking

This module supports physique goals and body composition tracking.

Long-term capabilities include:

- body weight;
- measurements such as chest, waist, hips, arms, thighs, and shoulders;
- optional body fat percentage;
- optional progress photos;
- phase tagging: bulk, cut, maintain;
- weight trend lines;
- measurement deltas;
- weekly rate of change;
- monthly physique progress reports;
- AI analysis with phase context.

Health owns authoritative body weight in M11. Broader physique measurements, photos, and phase tracking are future work.

### 6.10 Nutrition

Nutrition should stay practical and not become a full food database in early versions.

Long-term capabilities include:

- meal logging;
- estimated protein, carbs, fat, calories;
- water intake;
- daily targets;
- common meal templates;
- supplement routines as habits or quests;
- weekly meal prep planner;
- planned versus actual meal review;
- nutrition contribution to daily score;
- correlations with training, energy, and body metrics.

M10 Nutrition is a practical food/macro/hydration/supplement tracker with seeded/custom foods, historical food logs, reusable meals, targets, and operational summaries. It does not recreate a full food database, meal-planning platform, or prescription system.

### 6.11 Finance

Finance should exist, but V1 finance must be simple.

V1 finance capabilities include:

- manual income and expense entry;
- stable default categories;
- one user-level finance currency preference;
- selected-month income, expense, and net cash-flow totals;
- expense totals grouped by category;
- selected-month transactions;
- a small selected-year income, expense, and net cash-flow aggregate;
- optional transaction descriptions;
- no bank import, budgeting, or forecasting.

Future finance capabilities may include:

- Revolut CSV import;
- Raiffeisen CSV or XLS import;
- import preview;
- duplicate detection;
- merchant normalization;
- budget alerts;
- subscription manager;
- savings goals;
- net worth snapshots;
- AI spending summaries.

The richer finance scope is preserved, but it is not part of V1. M7 Simple
Finance is a lightweight personal tracker: authoritative transaction data is
used to derive monthly and yearly aggregates. It does not implement budgeting,
planned income, allowance tracking, or enterprise accounting concepts.

### 6.12 Study and project tracker

Study and project tracking supports academic progress and internship readiness.

Long-term capabilities include:

- subjects/modules;
- weekly study targets;
- study session logs;
- topic covered;
- method such as Pomodoro, deep work, review, lecture, or other;
- personal projects;
- project status: Idea, In Progress, Paused, Shipped;
- tech stack and GitHub link;
- work session logs;
- lifetime project hours;
- neglected subject detection;
- internship/portfolio summaries later.

This module is future scope. It should be added after tasks, habits, and XP are stable.

### 6.13 Focus sessions and time blocking

Focus support is related to study and projects.

Long-term capabilities include:

- Pomodoro timer;
- configurable focus duration;
- short and long breaks;
- auto-log completed sessions;
- manual override;
- connection to tasks, subjects, and projects;
- time-block calendar overlay later.

This module can be added before or alongside study/project tracking.

### 6.14 Journal and weekly intentions

M11 includes structured daily wellbeing; it is not a separate Wellbeing module. Free-form journals, guided prompts, draining-factor notes, and weekly intentions remain future work and require their own approved scope. They must not be treated as an M11 requirement or automatic AI context.

### 6.15 Calendar, Analytics, and AI

M12 Calendar is a first-class temporal overview. It owns native Calendar Events and projects dated authoritative records from other domains without duplicating their persistence; it does not schedule workouts or project ordinary Finance transactions. M13 then reviews the operational modules as one product. M14 Analytics follows real data accumulation and begins with deterministic domain analytics before justified cross-domain analysis.

AI is future work after Analytics; it has no milestone number. It should consume structured data and deterministic domain/analytics capabilities rather than calculate raw histories itself.

AI should be added only after clean data exists.

Long-term capabilities include:

- reactive chat;
- weekly reviews;
- proactive insight generation;
- trend explanations;
- confidence-aware recommendations;
- cited or explainable data references;
- finance summaries;
- physique progress reports;
- study neglect alerts;
- cross-domain correlation insights.

AI is not part of the M8-M14 implementation roadmap. Its architecture and implementation wait for separately approved post-Analytics scope.

### 6.16 PWA and mobile strategy

LifeOS is desktop-first but should support phone entry.

Long-term capabilities include:

- responsive layout;
- large mobile touch targets;
- quick-add forms;
- installable PWA manifest;
- simplified mobile dashboards;
- possible offline capture later.

V1 should include responsive basics and a PWA shell, not a full offline-first system.

### 6.17 Garmin future integration

Garmin is a future integration to support sleep, recovery, HRV, training load, and cardio/workout data.

Planned future capabilities include:

- Garmin Connect CSV import for sleep;
- workout import for cardio sessions;
- HRV and recovery import;
- optional body metric sync where available;
- coexistence between manual entries and imported data.

Future import milestones may define source metadata when that capability is
actually approved. M7 finance remains manual-only and has no import metadata.

### 6.18 Import/export strategy

Long-term capabilities include:

- CSV import for supported modules;
- finance import preview;
- duplicate detection;
- export to CSV or JSON;
- PDF summaries later;
- backup and restore.

Import/export is not a V1 requirement except basic future-proofing in data design.

### 6.19 Correlation engine and data confidence

Long-term analytics should include:

- rolling averages;
- before/after comparisons;
- correlations between modules;
- minimum sample thresholds;
- confidence labels such as low, medium, or high confidence;
- explanations of why a recommendation was made.

This should come after enough real data exists.

## 7. Product success criteria by approved roadmap phase

### 7.1 V1 success

V1 succeeds if:

- the user can manage tasks and habits daily without friction;
- the user can receive dependable in-app reminders;
- XP and progression feel motivating and trustworthy;
- manual finance tracking is quick enough to use;
- the dashboard shows what matters today;
- data integrity is strong;
- the codebase is clean enough to add modules later.

### 7.2 M8-M12 success

M8-M12 succeed when LifeOS captures useful structured Fitness, Nutrition, Health, and Calendar data through clear domain ownership, without premature generic analytics, AI, or integration architecture.

### 7.3 M13 success

M13 succeeds when the completed operational domains work together as one accessible, responsive, low-friction product.

### 7.4 M14 and later success

M14 succeeds when deterministic analytics answer real questions from accumulated historical data. AI remains later future work and succeeds only when it can use those deterministic capabilities safely and explainably.

## 8. What this vision document protects

This document protects the full LifeOS ambition from being accidentally deleted during V1 scoping. V1 documents may exclude modules from immediate build scope, but the long-term product still includes sleep, health, fitness, physique tracking, nutrition, study, projects, wellbeing, AI, Garmin, and advanced analytics.
