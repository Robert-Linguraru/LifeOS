# Milestone 5 — XP and Progression Specification

Milestone 5 is the XP and progression core vertical slice. It owns the append-only XP ledger, one current progression projection per user, Quest XP from Task and Habit completion, the daily cap, exact level and echelon rules, idempotency, concurrency-safe atomic persistence, current progression and history service queries, transition metadata, and the XP Progress Dashboard widget.

Persisted notifications belong to Milestone 6. Milestone 5 detects and reports level/echelon transitions and logs significant transitions where appropriate, but does not create `Notification` rows or require `INotificationService`.

## Scope boundary

Milestone 5 includes:

- positive `QuestCompletion` awards for new Task and Habit completion events;
- no automatic XP backfill for pre-Milestone-5 completions;
- Task completion hardening before XP integration: conditional user-scoped `Active -> Completed`, with only the winning transition initiating XP;
- HabitLog insertion as the Habit XP event boundary, with duplicate and concurrent inserts idempotent;
- authoritative source timestamps and user-local dates copied to the XP transaction;
- the XP Progress Dashboard widget and completion feedback, not a global/header XP chip;
- service-boundary XP history, newest first, immutable DTOs, with no history UI requirement.

DailyScore XP, streak-bonus XP, persisted notifications, achievements, badges, unlocks, privileges, manual adjustment, reversal, compensation commands, historical backfill, reconciliation jobs, outbox/message bus, background jobs, and Task/Habit undo or reopen are outside Milestone 5.

## Quest XP calculation

Use the existing Core enums `EstimatedTime` and `FrictionLevel`. Do not create XP-specific duplicates. Invalid enum values are rejected.

| EstimatedTime | Base XP |
|---|---:|
| `Under15Minutes` | 50 |
| `Between15And30Minutes` | 100 |
| `Between30And60Minutes` | 150 |
| `Over60Minutes` | 200 |

| FrictionLevel | Multiplier |
|---|---:|
| `Low` | 1.0 |
| `Medium` | 1.5 |
| `High` | 2.0 |

Raw Quest XP is `Base XP × Friction multiplier`. Quest XP calculations use decimal arithmetic. When a calculated XP value is not a whole number, round to the nearest whole XP using `MidpointRounding.AwayFromZero`. Do not use binary floating point. The canonical matrix is:

| EstimatedTime | Low | Medium | High |
|---|---:|---:|---:|
| Under 15 minutes | 50 | 75 | 100 |
| 15–30 minutes | 100 | 150 | 200 |
| 30–60 minutes | 150 | 225 | 300 |
| Over 60 minutes | 200 | 300 | 400 |

No DailyScore or streak-bonus XP is included in this calculation.

## Daily Quest cap

A user may receive at most 500 actual `QuestCompletion` XP for a user-local `BusinessDate`, across Task and Habit sources together. The authoritative amount already awarded for a target date is the sum of applicable positive Quest XP transactions in the ledger for that user and `BusinessDate`; cached progression daily fields never authorize exceeding the cap, including after a time-zone change.

If a raw award crosses the cap, grant only the remaining capacity. For example, 450 plus raw 100 awards 50 and reaches 500. This is not an exception and the source completion succeeds. At an exhausted cap, completion still succeeds, Habit streak behavior remains intact, raw XP may be returned, actual XP is zero, and no zero-XP transaction is created. Every positive actual award creates exactly one transaction; zero awards do not.

## Level and echelon rules

XP needed to advance from level `L` to `L + 1` is `150 + 30 × L`. Total lifetime XP required to reach level `N` is `15 × (N − 1) × (N + 10)`. Current level is the greatest level whose cumulative threshold is less than or equal to `TotalLifetimeXp`.

Level is at least 1. Calculations use exact arithmetic, are safe for every non-negative `long` lifetime XP value, do not use floating-point approximations, and have no undocumented maximum level.

| Level | Lifetime XP required |
|---:|---:|
| 1 | 0 |
| 2 | 180 |
| 10 | 2,700 |
| 25 | 12,600 |
| 50 | 44,100 |
| 100 | 163,350 |

| Level | Echelon |
|---|---|
| 1–9 | Iron |
| 10–19 | Bronze |
| 20–29 | Silver |
| 30–39 | Gold |
| 40–49 | Platinum |
| 50–74 | Onyx |
| 75–99 | Radiant |
| 100–124 | Apex |
| 125–149 | Celestial |
| 150–174 | Immortal |
| 175–199 | Abyssal |
| 200+ | Ascendant |

Echelon is derived from level. Milestone 5 introduces no echelon privileges, multipliers, unlocks, achievements, or badges. Award results expose previous/current level and echelon plus transition flags.

## Persistence contract

`XpTransaction : UserOwnedEntity` is the append-only authoritative historical ledger:

- `Id`, `UserId`, `Source`, `SourceType`, `SourceEntityId`, `XpAmount`, `OccurredAtUtc`, `BusinessDate`, `IdempotencyKey`, `Notes`, plus the repository's inherited audit/lifecycle fields;
- `Source = QuestCompletion`, `SourceType = Task | Habit`, required `SourceEntityId`;
- `XpAmount` is the actual positive amount awarded after the cap, never raw uncapped XP;
- source completion `CompletedAtUtc`/`CompletedDate` or `HabitLog.CompletedAtUtc`/`CompletionDate` are authoritative for `OccurredAtUtc`/`BusinessDate`;
- `IdempotencyKey` is required for Milestone 5 Quest awards, max 200 characters, and `Notes` is optional, max 500 characters;
- no Task/Habit navigation, streak data, DailyScore state, notification state, editable status, or reversal API.

`UserProgression : UserOwnedEntity` is an atomically maintained materialized current-state projection, not an independently editable source of truth. It is conceptually reconstructable from the ledger, but Milestone 5 provides no repair command:

- inherited `Id` is the primary key;
- required unique `UserId`;
- `TotalLifetimeXp : long = 0`;
- `CurrentLevel : int = 1`;
- `CurrentEchelon : Echelon = Iron`;
- `DailyQuestXpToday : int = 0`;
- `DailyQuestXpDate : DateOnly? = null`, persisted as PostgreSQL `date`;
- `Version : long = 0`, non-negative and an EF concurrency token.

Invariants are non-negative lifetime XP, level at least 1, daily cache 0–500, non-negative version, and one progression row per user. There is no user-facing progression delete/reset lifecycle. Progression is lazily initialized on first access or award, race-safely; absence is not a normal application error and no seeding pipeline is added.

`IXpRepository` is the single aggregate persistence boundary for `XpTransaction` and `UserProgression`. It owns idempotency lookup, progression retrieval/initialization, ledger business-date sums, history, and atomic positive-award commit. It does not resolve the current user or calculate Quest XP, level, echelon, or cap business rules. No generic repository or Unit of Work is introduced.

A positive XP transaction and progression mutation commit atomically. Existing XP transactions are append-only: persistence rejects Modified or Deleted states before generic soft-delete conversion. No normal update/archive/delete methods exist. The schema must not prohibit future compensating negative transactions solely by requiring `XpAmount > 0`; Milestone 5 service behavior still awards positive Quest XP only.

## Idempotency and concurrency

Keys are generated server-side, culture-invariant, and are not supplied by Web:

- Task: `TaskComplete:{TaskId:D}`;
- Habit: `HabitComplete:{HabitId:D}:{CompletionDate:yyyy-MM-dd}`.

The chosen standard .NET `D` formatting behavior is used consistently. Uniqueness is `(UserId, IdempotencyKey)` for non-null keys. A pre-existing matching transaction or a uniqueness race is a safe idempotent result: it does not increment progression twice, change the original transaction, or expose a normal duplicate exception. `DuplicateXPTransactionException`, `DailyQuestCapReachedException`, and `ProgressionNotFoundException` are not part of the normal Milestone 5 contract.

`UserProgression.Version` is incremented on successful mutation. Different events for the same user require optimistic concurrency. On a progression conflict, reread authoritative state, recalculate the ledger daily sum, remaining cap, actual award, lifetime XP, level, and echelon, then retry the complete commit. The bounded limit is a maximum of 3 attempts; unrelated database errors are not retried.

Source completion is persisted first, then XP is invoked synchronously. XP transaction plus progression is atomic with each other, but not with Task/Habit completion. If XP fails after bounded retries, completion remains successful and the result reports an XP-specific partial-success warning; it is not rolled back or reported as a failed completion. Structured logging supports diagnosis; durable reconciliation is technical debt.

## Service and Dashboard boundaries

`IXpService`/`XpService` owns current-user validation, calculation, idempotency, cap enforcement, progression rules, retry orchestration, transition detection, and DTO composition. It exposes `AwardQuestXpAsync`, `GetProgressionAsync`, and `GetXpHistoryAsync`. Web never calls `IXpRepository`. Task/Habit services initiate awards. No Milestone 5 methods exist for DailyScore, streak bonus, adjustment, reversal, compensation, notification creation, or editing.

`DashboardService` retains widget-specific architecture and delegates `GetXpWidgetAsync` to `IXpService`. It does not query `IXpRepository`, calculate progression, or independently determine the authoritative local date. The widget displays level, echelon, lifetime XP, today's actual Quest XP, `x of 500`, remaining Quest XP, normalized progress, and accessible progress semantics. No global/header XP chip is added.

## Testing and acceptance

Tests cover all 12 XP combinations, invalid enums, cap underflow/crossing/exhaustion, exact level and echelon boundaries and large `long` values, deterministic keys, defaults and mappings, uniqueness, concurrency, append-only behavior, no polymorphic FK, service idempotency/retries/history/time-zone behavior, PostgreSQL transactions/races/DateOnly/DateTimeOffset/bigint/user isolation, Task/Habit integration, partial success, and responsive accessible widget states. PostgreSQL/Testcontainers is used for relational behavior; EF InMemory is not used for it. Manual browser verification confirms `XpWidget` is a Razor component rather than a literal custom element, including loading/error/retry and refresh behavior. No UI test package is added solely for this ticket.