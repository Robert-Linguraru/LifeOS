# M8 — Strength & Calisthenics Training Architecture Contract

## 1. Authority and scope

This document freezes the M8 implementation contract before production models, persistence, mappings, seed data, or migrations are created. The canonical product requirements remain `bible/docs/product/16-post-m7-domain-roadmap.md`; this document defines the M8 architectural detail.

M8 is **Strength & Calisthenics Training**. M9 owns running, sprinting, intervals, walking, steps, sports, other activities, broader Fitness history, Fitness Goals, pinned PRs, Fitness summaries, and the Fitness Dashboard widget. M8 does not absorb those capabilities.

The governing principle is structured authoritative data now, deeper analytics later. M8 does not introduce a generic metric engine, generic idempotency subsystem, state-machine framework, AI, device/import schemas, or M13 visual redesign.

## 2. Domain terminology and ownership

```text
Exercise
  ??? ExerciseSecondaryMuscleGroup

WorkoutTemplate
  ??? WorkoutTemplateExercise ??? Exercise

WorkoutSession
  ??? WorkoutSessionExercise ??? Exercise
        ??? WorkoutSet
```

- `Exercise` is global seeded reference data. Calisthenics uses this same model and library.
- `ExerciseSecondaryMuscleGroup` is the normalized secondary-muscle relationship.
- `WorkoutTemplate` is user-owned reusable configuration.
- `WorkoutTemplateExercise` is an ordered template child.
- `WorkoutSession` is user-owned persisted workout state and historical record.
- `WorkoutSessionExercise` is the session-side historical exercise snapshot.
- `WorkoutSet` is one persisted individual set.
- A Custom Workout is a session without a template. It does not create custom Exercises.

Templates are recipes. Sessions are records of what actually happened. No template edit may rewrite a completed session.

## 3. Exercise Library contract

M8 contains approximately 80–120 deterministic seeded Exercises. Every Exercise has an explicit stable GUID and deterministic definition. Seed data will be added later through EF Core seed configuration. IDs are never derived from names, generated at runtime, or assigned to users. An existing Exercise identity must never be reused for a different exercise.

Metadata concepts are fixed for M8:

### Muscle groups

Chest; Back/Lats; Shoulders; Biceps; Triceps; Forearms; Quadriceps; Hamstrings; Glutes; Calves; Core; Full Body.

### Equipment

Barbell; Dumbbell; Cable; Machine; Smith Machine; Bodyweight; Pull-up Bar; Dip Bars; Kettlebell; Resistance Band; Other.

### Movement patterns

Horizontal Push; Vertical Push; Horizontal Pull; Vertical Pull; Squat; Hinge; Lunge; Carry; Core; Isolation; Full Body; Skill.

These are known domain values, not a configurable taxonomy. Custom Exercises are future work.

## 4. Logging modes and measurements

M8 supports exactly these seven Exercise logging modes:

1. Weight + Reps
2. Bodyweight + Reps
3. Added Weight + Reps
4. Assisted Weight + Reps
5. Reps Only
6. Duration
7. Weight + Duration

The persistence direction is nullable scalar `WeightKg`, nullable integer `Repetitions`, and nullable integer `DurationSeconds`; interpretation is determined by the Exercise logging mode snapshotted onto the session exercise. No generic measurement definitions, EAV, JSON metric payloads, or user-configurable logging modes are permitted.

| Mode | Required completed values | Not applicable |
|---|---|---|
| Weight + Reps | `WeightKg > 0`, `Repetitions > 0` | Duration |
| Bodyweight + Reps | `Repetitions > 0` | Weight, Duration |
| Added Weight + Reps | `WeightKg > 0`, `Repetitions > 0` | Duration |
| Assisted Weight + Reps | `WeightKg > 0`, `Repetitions > 0` | Duration |
| Reps Only | `Repetitions > 0` | Weight, Duration |
| Duration | `DurationSeconds > 0` | Weight, Repetitions |
| Weight + Duration | `WeightKg > 0`, `DurationSeconds > 0` | Repetitions |

For Assisted Weight + Reps, `WeightKg` represents assistance weight. The exact field name may be refined during implementation, but the mode must not be interpreted as normal load.

Units are fixed: weight is kilograms with decimal precision suitable for fractional plates; repetitions are integers; duration and rest are whole seconds. M8 has no lb/kg preference, unit-conversion infrastructure, or user bodyweight capture. Bodyweight exercises never fabricate volume from unknown body weight.

## 5. Workout Templates

A `WorkoutTemplate` is user-owned reusable configuration. It contains a name, ordered `WorkoutTemplateExercise` children, target set count, optional target rep minimum/maximum, and default rest duration per exercise.

Supported operations are create, rename, reorder exercises, modify targets, modify rest duration, and delete. Template editing uses a persisted `Version` optimistic-concurrency token. A stale edit must not silently overwrite newer state.

Validation:

- target set count is greater than zero;
- rep minimum and maximum are either both absent or both present;
- minimum is less than or equal to maximum;
- rep targets apply only to compatible rep-based logging modes;
- zero rest means no automatic rest timer;
- duplicate Exercise identities may appear more than once in a Template.

Templates are not programs, periodization plans, automatic progression plans, schedules, or historical performance. M8 does not introduce any of those concepts.

## 6. Workout Sessions and lifecycle

Starting either a Template workout or Custom Workout immediately creates a persisted `WorkoutSession`. The active state must survive closing LifeOS and returning later; Blazor component memory is never authoritative.

The lifecycle is exactly:

```text
InProgress
   ??? Completed
   ??? Discarded
```

There is no Paused state, no separate Cancelled state, and no state-machine framework. Completed and Discarded are terminal for normal M8 operations.

A session has a persisted `Version` concurrency token. Aggregate mutations operate against an expected version, including exercise changes, substitution, skip, set add/edit/remove, set completion, timer mutations, completion, and discard. A stale caller receives a concurrency conflict; the intended UI recovery is reload authoritative state and tell the user that the workout changed elsewhere.

### One active session

Before creation, the application checks for the current user's non-deleted `InProgress` strength session. If one exists, the operation returns enough information for the UI to resume it. PostgreSQL will ultimately enforce the authoritative partial unique constraint/index equivalent to one non-deleted `InProgress` `WorkoutSession` per `UserId`.

If concurrent starts race, one session wins and the other operation reloads/returns the existing active session. Two active sessions must never remain. Workout start does not use a generic idempotency subsystem.

## 7. Historical snapshots

Starting from a Template snapshots at least:

### Session snapshot

- workout/template name.

### Session-exercise snapshot

- exercise order;
- original Exercise identity and name;
- actual Exercise identity and name;
- logging mode;
- target set count;
- target rep range;
- default rest duration.

Substitution changes only the actual session exercise. For example, Barbell Bench Press may be performed as Dumbbell Bench Press while the Template remains untouched. Historical screens use session snapshots, never current mutable Template values.

Do not snapshot the complete serialized Template, Template version, muscle/equipment metadata merely for duplication, current PR values, or future analytics.

## 8. Sets, warm-ups, and working sets

`WorkoutSet` contains:

```text
set order
Warm-up or Working
nullable WeightKg
nullable Repetitions
nullable DurationSeconds
nullable CompletedAtUtc
```

A set without `CompletedAtUtc` is a persisted draft. During an active session the user can add, edit, remove, and complete sets. Completed sets must satisfy their session exercise's snapshotted logging mode. Drafts do not count toward history or performance and are removed when the workout is completed. Child mutations increment the aggregate Session version.

Warm-up and Working are both historical evidence. Working sets drive relevant progression and PR semantics. Warm-ups never create equivalent performance achievements.

## 9. Substitution and skip

Substitution affects only the session and records both original and actual Exercise. Completed performance belongs to the actual Exercise. Substitution is rejected when completed sets already exist for that session exercise unless those sets are removed first.

Skipping affects only the session. A skipped exercise remains represented historically as skipped and cannot be skipped while completed sets remain. Template state is never changed by substitution or skip.

## 10. Previous performance and history

Previous performance means the most recent earlier **Completed** strength Session for the same user and same actual Exercise. The current Session is excluded. Discarded Sessions, soft-deleted evidence, and skipped exercises are excluded. The active-workout projection emphasizes completed Working sets; full historical detail retains Warm-ups.

Previous performance for several Exercises must use an efficient bounded/batched projection, not an N+1 query per Exercise. This is an M8 domain query requirement, not generic Analytics infrastructure. Exercise History is an M8 capability.

## 11. Persisted rest timer

The rest timer is part of persisted active-workout state. Conceptually persist configured/current duration, running timer end UTC instant, and paused remaining seconds. The UI may use a local timer to render a countdown, but must not write every second, depend on component memory, use Hangfire, or require a permanently connected Blazor circuit. Reloading the active workout reconstructs the timer from persisted state.

Required commands are pause/resume, reset, skip, and adjust duration. Completing a set may optionally start the snapshotted default rest timer. Zero configured rest means no automatic timer.

## 12. Completion and discard

Completion is explicit and requires at least one completed set. It removes unfinished draft sets, marks untouched session exercises skipped where appropriate, stops/clears the active timer, records completion timestamp, optionally captures Session Feeling, and transitions to `Completed`.

Session Feeling values are Weak, Normal, Good, and Great.

Discard explicitly transitions `InProgress ? Discarded`, records discard time, clears active timer state, remains persisted, does not appear in normal Fitness history, and does not contribute to PR/progression calculations. Discard is not soft deletion.

For M8, Completed Sessions are read-only through normal application workflows. This is an application/service lifecycle rule, not irreversible database-level append-only semantics; M9 must remain able to define approved correction/deletion behavior.

## 13. M8 strength PR rules

PRs are derived from authoritative Session/Set history. There is no authoritative mutable `CurrentPR`, `ExerciseBest`, or `UserExercisePR` table. Only completed Working sets qualify; Warm-ups never qualify; ties are not new PRs; only strictly better results qualify; first qualifying evidence establishes a first record; immediate feedback is non-blocking.

| Logging mode | M8 strength PR semantics |
|---|---|
| Weight + Reps | Heaviest weight; best reps at an exact weight |
| Bodyweight + Reps | Most reps |
| Added Weight + Reps | Heaviest added weight; best reps at an exact added weight |
| Assisted Weight + Reps | No M8/V1 PR |
| Reps Only | Most reps |
| Duration | Longest duration |
| Weight + Duration | Heaviest weight; longest duration at an exact weight |

M8 owns deterministic strength PR evaluation, immediate strength feedback, supported completion-summary PRs, and derived best information in Exercise History. M9 owns broader Fitness PRs, running/sprinting PRs, pinned/favourite PRs, Fitness Goals, unified Fitness history, Fitness summaries, and the Fitness Dashboard widget.

## 14. Training-volume semantics

M8 must not produce one universal cross-mode workout-volume number. Meaningful compatible measures are:

- Weight + Reps: weight × reps;
- Added Weight + Reps: added load × reps, semantically distinct;
- Bodyweight + Reps: completed reps, not guessed bodyweight tonnage;
- Reps Only: completed reps;
- Duration: completed duration;
- Weight + Duration: appropriate load/duration information, not arbitrary kg-seconds;
- Assisted Weight: no universal volume formula.

Completion summaries may show compatible measures but never combine incomparable metrics merely to produce one total.

## 15. Persistence direction

The eventual EF/PostgreSQL direction is:

- global seeded `Exercise`;
- normalized secondary-muscle relationship;
- user-owned `WorkoutTemplate`, `WorkoutTemplateExercise`, `WorkoutSession`, `WorkoutSessionExercise`, and `WorkoutSet` records;
- restrictive deletion behavior for historical/reference foreign keys;
- persisted Template and Session aggregate concurrency versions;
- partial unique active-Session invariant per user;
- ordered-child uniqueness where appropriate;
- useful user/date/exercise/history indexes;
- decimal kilogram precision;
- integration with existing audit and soft-delete infrastructure;
- database constraints for simple local invariants.

Where practical, child ownership is enforceable through parent/user relationships so cross-user aggregate corruption cannot be persisted. No migration names or generated SQL are prescribed here.

## 16. Testing contract

Future M8 tickets must cover appropriate pure-rule, EF model, service, PostgreSQL/Testcontainers integration, and targeted bUnit layers. Critical scenarios include deterministic Exercise seed identities; one-active-workout race; active-session persistence across fresh contexts; stale Session/Template versions; historical snapshots; user isolation; logging-mode validation; substitution/skip; Warm-up/Working behavior; completion/discard; previous performance batching; rest-timer reconstruction; PR rules; and exclusion of discarded/deleted evidence from derived results.

Browser automation is not introduced by M8 Ticket 1.

## 17. Explicit exclusions

M8 excludes custom Exercises; multi-week programs; periodization; automatic progression; RPE/RIR; estimated 1RM; M9 running, sprinting, walking, steps, sports, and other activities; broader Fitness Goals; unified Fitness history; the Fitness Dashboard widget; Workout XP; Calendar workout scheduling; bodyweight capture; Garmin/Strava/device imports; GPS; heart rate; calories; generic Analytics; AI; generic metrics; lb/unit preferences; full historical correction UI; and M13-level visual redesign.
