# 16 - Post-M7 Domain Roadmap and Requirements

## Purpose and status

This is the canonical active requirements document for planned Milestones 8–14. It supplements the implemented V1 documentation; it does not change the completed M0–M7 contract.

**Implemented through M7:** foundation, Tasks, Habits, XP/progression, one-time in-app Reminders/Notifications, Dashboard, Settings, and Simple Finance.

**M7 Simple Finance:** manual Income and Expense transactions, stable default categories, transaction CRUD, monthly income/expenses/net and expense-category breakdown, monthly history, small yearly income/expenses/net summary, user-level currency preference, and Dashboard widget. It deliberately excludes budgets, spending plans, allowances, transaction currencies, FX, imports, accounting/ledger infrastructure, and advanced finance analytics.

The governing principle for the planned domains is:

> Capture high-quality structured data and build good domain architecture now; build deeper analytics and AI later.

Do not introduce generic analytics or cross-domain metric engines, AI orchestration or AI-specific fields, device-import/provenance schemas, configurable measurement engines, or abstractions without a concrete V1 requirement.

## Approved roadmap

| Milestone | Status | Scope |
|---|---|---|
| M8 | Planned | Strength & Calisthenics Training |
| M9 | Planned | Activity & Fitness Progression |
| M10 | Planned | Nutrition |
| M11 | Planned | Health |
| M12 | Planned | Calendar |
| M13 | Planned | V1 UI/UX Overhaul & Polish |
| M14 | Planned | Analytics |
| Future | Deferred | AI, integrations, accolades/achievements, and other approved future work |

## M8 — Strength & Calisthenics Training

Fitness is a major domain, not merely a gym tracker. M8 establishes the deep shared strength and calisthenics foundation.

### Exercise library and logging

Seed approximately 80–120 common gym/strength and calisthenics exercises in one shared Exercise Library. Each exercise has stable identity, name, primary muscle group, optional secondary muscle groups, equipment, movement pattern/category, and logging mode. Reasonable muscle groups are Chest, Back/Lats, Shoulders, Biceps, Triceps, Forearms, Quadriceps, Hamstrings, Glutes, Calves, Core, and Full Body. Equipment includes Barbell, Dumbbell, Cable, Machine, Smith Machine, Bodyweight, Pull-up Bar, Dip Bars, Kettlebell, Resistance Band, and Other.

Calisthenics uses the same architecture and library. User-created exercises are future work.

Use a small known logging-mode set, not a configurable metric engine:

- Weight + Reps;
- Bodyweight + Reps;
- Added Weight + Reps;
- Assisted Weight + Reps;
- Reps Only;
- Duration;
- Weight + Duration.

### Templates and sessions

Workout Templates are reusable recipes with a name and ordered exercises. Each template exercise has target sets, optional target rep range, and default rest duration. Users can create, rename, reorder, modify, and delete templates. Templates do not hold authoritative historical performance, and later edits never rewrite completed sessions. Multi-week programming, periodization, automated progression, and AI-generated plans are deferred.

A Workout Session records what actually occurred:

`Template → Start Workout → Active Workout → Log Sets → Complete → Summary → History`

Users can also start a Custom Workout and choose library exercises; this does not create custom exercises. Saving a useful custom workout as a template is future enhancement. Only one active strength/calisthenics workout is preferred for V1. Active sessions and logged progress are persisted and survive closing or returning to LifeOS; component memory is never authoritative.

The active logging UI is purpose-built for training, supports measurements appropriate to the exercise mode, adding/removing/editing/completing sets, easy exercise navigation, and visible previous performance. It is not generic CRUD.

Sets are at least Warm-up or Working. Both remain historical; PR/progression/volume semantics normally use Working sets. A template exercise may be substituted or skipped for that session without changing its template, and the session records what actually happened.

Completing a set may start its configured rest timer. The timer supports pause where appropriate, reset, skip, and duration adjustment; no background-job architecture is required. RPE/RIR are deferred. Completion captures an optional quick Session Feeling: Weak, Normal, Good, or Great.

The lifecycle is `In Progress → Completed` or `In Progress → Discarded`. Completion is explicit and summarizes template/workout name, duration, completed exercises, working sets, meaningful comparable training volume where applicable, supported PRs, and Session Feeling. Do not combine incomparable metrics into universal volume. Discarded sessions are excluded from normal history/progression. Completed sessions are historical truth and show actual exercises/sets; exercise history is available inside and outside an active session.

## M9 — Activity & Fitness Progression

M9 broadens Fitness while retaining authoritative, domain-specific records. It adds running, sprinting, intervals, intentional walking, daily steps, sport sessions, other activities, optional perceived intensity, derived PRs, Fitness Goals, unified Fitness history, weekly/monthly operational summaries, and a Fitness Dashboard widget.

Runs, sprints, and intervals are one coherent capability. Runs record date, optional start time, subtype, distance, duration, derived pace, and optional intensity. Sprint/interval detail may record distance, time, and recovery, but simple sprint sessions without intervals are valid. Do not add GPS, heart-rate zones, cadence, elevation, calories, weather, shoes, or device provenance.

Intentional walking records date, optional start time, duration, optional distance, derived pace when possible, and optional intensity. Distance is not required. Daily manual steps are a lightweight daily Fitness metric; device-populated steps are future work.

Sport Session is generic: sport type, date, optional start time, duration, and optional intensity. Values include Football, Table Tennis, Basketball, Tennis, Badminton, Padel, Volleyball, and Other (with a short name). Do not add sport-specific statistics. Other activities are lightweight categories such as Cycling, Swimming, Hiking, Mobility, Yoga, and Other. Perceived Intensity is optional: Easy, Moderate, Hard, or Max Effort; never fabricate a default.

The unified Fitness history/calendar is a read projection across authoritative Fitness records, not a duplicate `FitnessTimeline` persistence table. Useful navigation can include All, Strength, Running, Walking, Sports, and Other. Completed Fitness records may be corrected/deleted; derived PRs and goals must remain consistent.

### Personal Records and goals

PRs are automatically derived achievements from completed authoritative data. Only strictly better results are new PRs; ties are not. Strength evaluates Working sets only. Warm-ups never create PRs; assisted-weight sets have no M9 PRs. Immediate workout feedback is non-blocking. Records are recalculable after history correction/deletion and are not independently editable mutable truth. Users may pin/favourite derived records.

V1 PR semantics:

- Weight + Reps: heaviest weight and best reps at a weight.
- Bodyweight + Reps and Reps Only: most reps.
- Added Weight + Reps: heaviest added weight and best reps at that weight.
- Duration: longest duration.
- Weight + Duration: sensible heaviest-weight and/or longest-duration-at-weight semantics.
- Running: longest run and fastest genuinely evidenced standard distances (1 km, 5 km, 10 km, Half Marathon, Marathon).
- Sprint/interval: 100 m, 200 m, and 400 m only when recorded interval detail proves it.
- Walking/steps: most steps on one day.
- Sports/Other: no V1 PR system.

Do not infer a standard-distance PR from a longer unsplit manual run. Estimated 1RM and advanced performance analytics are deferred.

Fitness Goals are user-created measurable performance/outcome goals evaluated from authoritative data, for example a bench weight, pull-up count, 5K time, 100 m time, or deliberately-created daily step target. A result can both create a PR and complete a goal. Fitness Goals are not habits or cumulative-volume/frequency challenges; monthly distance, weekly workout count, and lifetime-workout targets belong respectively to future achievements or Habits.

### Summaries and widget

Weekly/monthly operational summaries can show activity count, strength workouts, running activity/distance, walking/steps, sports/other, and notable PRs. They are not generic Analytics. The compact Fitness Dashboard widget shows this-week activity count, a Start Workout shortcut, steps, and the most recent PR. The Fitness landing page is the operational hub.

## M10 — Nutrition

Nutrition V1 is a frictionless food, macronutrient, hydration, and supplement tracker—not a MyFitnessPal replacement.

Seed approximately 100 common foods across meat/fish, eggs/dairy, grains, pasta/rice, bread, fruit, vegetables, legumes, nuts/seeds, fats, snacks, and selected useful products. Food definitions provide Calories, Protein, Carbohydrates, and Fat, primarily per 100 g. Users can create reusable custom foods.

A Food definition is reusable reference data; a Food Log Entry is historical consumption. Editing a Food must not silently alter historical log truth. Daily entry is grouped into Breakfast, Lunch, Dinner, Snack, and optional Other. Grams are universal; known practical conversions such as egg, slice, scoop, or serving may help, without a configurable unit-conversion engine. Recent and favourite foods are required.

Users can save reusable meals made of constituent foods. Logging one adds meaningful constituent historical entries, not only an unexplained aggregate. Quick Add records a clearly distinguished aggregate calories/macros entry when food reconstruction is undesirable.

Users configure daily calories, protein, carbohydrate, fat, and water targets. LifeOS displays actual versus target but does not prescribe targets or apply binary success/failure semantics. Hydration supports frictionless increments such as +250 ml, +500 ml, and +1 L, optionally with a preferred amount. Users configure routine supplements; daily tracking is Taken/Not Taken only, with no dosage, pill counts, interactions, recommendations, or separate supplement streak engine.

Nutrition Today combines food log, calories/macros, hydration, and supplements. Weekly/monthly operational summaries cover calorie totals/averages, macros, hydration consistency, and target comparison; they are not generic Analytics. The compact Nutrition Dashboard widget shows daily calories, protein, water, and Log Food. Barcode scanning, external food databases, food recognition/photos, deep micronutrients, sophisticated recipes/meal planning, dietary prescription, weight algorithms, and deep analytics are deferred.

## M11 — Health

Health V1 is lightweight, manual, non-medical, and low-friction.

Health is the authoritative owner of body weight. It supports manual dated weight entries, backfilling, history, a body-weight goal, and simple deterministic summaries such as current weight and 30-day change. Nutrition and Fitness do not own body weight.

Sleep supports bedtime, wake time, calculated duration, optional quick quality (Poor, Okay, Good, Great), and backfilling. Daily Wellbeing is an optional rapid dated check-in with Energy (Low, Normal, High, Very High), Mood (Poor, Okay, Good, Great), Stress (Low, Moderate, High, Very High), and Overall (Poor, Okay, Good, Great). No free-text is required. Today is quick capture; History is review; analytics waits for M14.

The compact Health Dashboard widget projects domain data only: sleep, wellbeing, and optionally weight; absent data displays `No check-in yet`. Do not add heart rate, blood pressure, HRV, oxygen saturation, symptoms, diagnoses, medical records, medication, lab data, medical recommendations, injury diagnosis, readiness scores, or wearable sync.

## M12 — Calendar

Calendar is a first-class temporal overview: **what happened, what is happening, and what is coming up across time**. Dashboard answers what matters now; Analytics interprets history. Calendar defaults to Month and also provides Week and Day views.

Calendar owns native Calendar Events only. Event fields are Title, Date, optional Start Time, and optional End Time. It does not own Tasks, Habits, Fitness, Nutrition, or Health records and must not duplicate them into Calendar persistence. It projects authoritative domain records into the temporal overview.

Tasks with meaningful dates/due dates are projected; undated tasks are not. Month gives a compact count and Day gives individual dated tasks, linking to their existing edit/detail page. `+ Task` from a selected Calendar date navigates to the existing Task creation workflow with DueDate prefilled. Habits use existing scheduling/completion semantics: Month shows a compact completion ratio, Day shows individual habits/status, and Calendar never invents recurrence logic.

Calendar observes Fitness and does not schedule/control it. Active, completed, and recorded activities appear on their authoritative dates; discarded workouts are absent consistently with Fitness history. Use `Open Fitness` navigation for any date. Calendar has no Start Workout action and never assigns workouts to future dates.

Nutrition Month shows a tiny indicator only when Nutrition data exists; it shows no Nutrition indicator when no data exists. Day shows a small daily calories/water/supplement status summary and `Open Nutrition`; when no data exists, Day may show `No data recorded` with `Open Nutrition`. Health follows the same pattern: Month shows a Health indicator only when Health data exists and no indicator when it does not; Day shows weight/sleep/wellbeing summary plus `Open Health`, or may show `No data recorded` with `Open Health`. Missing optional data is not failure. Finance transactions have no Calendar V1 projection; users can make native events such as Salary Day.

Day navigation shortcuts lead to Tasks, Habits, Fitness, Nutrition, and Health. Quick Add supports native Event and existing Task creation only. No V1 filters, external sync, invitations/attendees, advanced recurrence, drag/drop scheduling, advanced time blocking, Calendar-created Fitness/Nutrition/Health records, or workout scheduling/programming.

## M13 — V1 UI/UX Overhaul & Polish

M13 happens only after the operational V1 domains above. It reviews LifeOS as one product: global navigation, responsive/mobile use, hierarchy, component consistency, Dashboard composition, forms and quick actions, empty/loading/error states, accessibility, histories/timelines, landing pages, design language, and interaction friction. Reference applications are inspiration, not cloning instructions. The result should be seriously usable before Analytics.

## M14 — Analytics

Analytics deliberately follows UI/UX polish and a period of real use. It begins with useful deterministic domain analytics, then meaningful cross-domain analysis driven by accumulated questions and data. Potential questions include strength progression, sleep change, nutrition-target consistency, spending change, workout performance after poor sleep, wellbeing/activity relationships, and relationships across Fitness, Nutrition, Health, Habits, Tasks/productivity, and Finance.

Do not design the complete analytics architecture now. The intended flow is:

`Structured LifeOS Data → Domain Business Logic → Deterministic Analytics → Analytics Capabilities → Future AI`

## Deferred and future work

AI is post-Analytics future work, has no milestone number, and should consume structured data, domain capabilities, deterministic analytics, and retrieval/query capabilities—not calculate enormous raw histories in prompts.

A future cross-domain Accolades/Achievements system may include lifetime steps, completed-task counts, habit-streak milestones, and workout-count milestones. It is distinct from Fitness Goals; do not force cumulative achievement semantics into Fitness Goals.

Also deferred: Garmin, Strava, wearables, health platforms, device-derived steps, heart rate, GPS, device sleep, imported Health/Fitness data, Google/Outlook/Teams calendar integration, large food databases, barcode lookup, and their external IDs, provenance, synchronization, deduplication, or import schemas. Such schemas require a separately approved future milestone.
