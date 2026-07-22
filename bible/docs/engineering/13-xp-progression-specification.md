# XP & Progression Specification

## Purpose

This document defines the complete progression system used by LifeOS.

It acts as the single source of truth for:

- XP calculation
- Daily XP limits
- Level progression
- Echelon progression
- Streak rewards
- Future Daily Score rewards

No implementation may deviate from this specification without updating this document.

---

# XP Sources

LifeOS awards XP from three sources.

| Source | V1 | Future |
|---------|:--:|:------:|
| Quest Completion | ✅ | |
| Daily Score | | ✅ |
| Streak Bonus | | ✅ |

---

# Quest XP

Quest XP is awarded immediately when a Task or Habit is completed.

Quest XP is calculated using:

```
Estimated Time × Friction Multiplier
```

---

# Estimated Time Base XP

| Estimated Time | Base XP |
|----------------|--------:|
| Under 15 Minutes | 50 |
| 15–30 Minutes | 100 |
| 30–60 Minutes | 150 |
| Over 60 Minutes | 200 |

---

# Friction Multiplier

| Friction | Multiplier |
|-----------|-----------:|
| Low | 1.0x |
| Medium | 1.5x |
| High | 2.0x |

---

# Final Quest XP Formula

```
Quest XP = Base XP × Friction Multiplier
```

Examples

| Task | XP |
|------|---:|
| Drink Water | 50 |
| Morning Journal | 75 |
| 45 Minute Workout | 300 |
| 90 Minute Deep Work Session | 400 |

---

# Daily Quest XP Cap

Daily Quest XP is capped.

```
Maximum Quest XP Per Day = 500 XP
```

After reaching the cap:

- Tasks still complete.
- Habits still complete.
- Streaks still update.
- No additional Quest XP is awarded.

---

# XP Transaction Rules

Every XP award must:

- create exactly one XPTransaction;
- contain the source;
- contain the source entity;
- contain the business date;
- contain the awarded amount;
- be idempotent.

XP is never edited manually.

Corrections must create compensating transactions.

---

# User Progression

Every user owns exactly one UserProgression record.

The record stores:

- TotalLifetimeXP
- CurrentLevel
- CurrentEchelon
- DailyQuestXPToday
- DailyQuestXPDate

UserProgression is updated atomically with XPTransaction creation.

---

# Level Progression

Levels are based on total lifetime XP.

Formula:

```
XP Required for Level N

Σ(Level × 30 + 150)
```

Reference values

| Level | Approx. XP |
|--------|-----------:|
| 2 | 180 |
| 10 | 2,850 |
| 25 | 13,800 |
| 50 | 41,000 |
| 100 | 163,500 |

Levels never decrease.

---

# Echelons

| Level Range | Echelon |
|--------------|----------|
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

Echelons are cosmetic only.

Changing echelon creates a notification.

---

# V1 Streak Rules

V1 supports only Daily Streaks.

A streak:

- increases when the habit is completed on consecutive local dates;
- resets when a required day is missed;
- uses the user's configured time zone;
- does not award XP in V1.

---

# Future Streak Rewards

Future versions introduce:

- Daily Streak Bonus
- Momentum Streak
- Weekly Streak

Daily bonus:

```
25 XP per active streak

Maximum 100 XP/day
```

---

# Future Daily Score

Daily Score is intentionally excluded from V1.

Future formula:

| Module | Weight |
|---------|-------:|
| Habits | 30 |
| Tasks | 20 |
| Sleep | 15 |
| Workout | 15 |
| Finance | 10 |
| Nutrition | 10 |

Rules

- Modules not configured by the user are excluded from the denominator.
- Daily Score is calculated by a scheduled background job.
- Daily Score awards XP through XPService.

---

# Business Rules

- XP is awarded only through XPService.
- UI never modifies XP.
- XPTransaction is append-only.
- UserProgression is derived from XPTransactions.
- Duplicate completions must never create duplicate XP.
- Level calculations must be deterministic.
- Echelon calculations must be deterministic.
- All XP calculations must be unit tested before release.