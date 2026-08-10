# Technical Debt

This document tracks intentional design compromises and future refactoring work.

A technical debt item should only be addressed when it provides a measurable improvement in maintainability, readability, scalability or developer productivity.

Avoid refactoring solely for the sake of abstraction.

---

# Architecture

## TD-001 — Extract persistence behaviors from AppDbContext

### Current implementation

`AppDbContext` currently contains:

- audit timestamp handling;
- soft delete conversion;
- soft delete query filters.

This implementation was chosen intentionally because it is simple and easy to understand while the architecture is still being established.

### Future improvement

As more modules are introduced, consider extracting these responsibilities into dedicated persistence behaviors (or EF Core interceptors if appropriate).

Examples:

- AuditBehavior
- SoftDeleteBehavior

`AppDbContext` should eventually become responsible only for:

- DbSets
- OnModelCreating
- persistence orchestration

### Priority

Medium

### Target

Reassess after multiple `BaseEntity` feature slices make the current lifecycle code difficult to maintain.

---

## TD-002 — Introduce a dedicated mapping layer

### Current implementation

Application services manually map entities to DTOs.

Example:

```csharp
return new UserSettingsDto
{
    UserId = settings.UserId,
    TimeZoneId = settings.TimeZoneId
};
```

### Future improvement

Introduce mapping extensions or mapper classes.

Example:

```csharp
public static class UserSettingsMappings
{
    public static UserSettingsDto ToDto(this UserSettings settings)
    {
        return new UserSettingsDto
        {
            UserId = settings.UserId,
            TimeZoneId = settings.TimeZoneId
        };
    }
}
```

Usage:

```csharp
return settings.ToDto();
```

### Benefits

- Keeps services focused on business logic.
- Centralizes mapping.
- Reduces duplicated code.
- Simplifies DTO evolution.

### Priority

Medium

### Target

Reassess when task mappings create meaningful duplication. This does not require unrelated UserSettings refactoring.

---



# Authentication

## TD-004 — Replace development current user with ASP.NET Identity

### Current implementation

The application currently runs in single-user development mode.

`DevelopmentCurrentUserService` returns a fixed Guid configured in application settings.

This allows user-owned features to be developed before authentication is introduced.

### Current limitations

- no authentication;
- no authorization;
- single logical user;
- development-only implementation.

### Future improvement

Replace `DevelopmentCurrentUserService` with an Identity-backed implementation that retrieves the authenticated user's Guid.

The `ICurrentUserService` abstraction should remain unchanged so that no application services or repositories require modification.

### Priority

High

### Target

Authentication / Identity milestone.

---

# Notes

Technical debt should only be addressed when there is a measurable improvement in:

- maintainability;
- readability;
- scalability;
- developer productivity.

Avoid introducing abstractions before they are justified by the application.

## TD-005 — Task list queries load all user tasks into memory

Status: Open
Priority: Low for V1; revisit as task volume grows
Introduced: Milestone 3

TaskService.GetTaskListAsync currently loads all non-deleted tasks for the
current user and performs status classification, date grouping, and sorting
in memory.

This is intentional for the initial Tasks vertical slice because it keeps
the repository contract and query behavior simple.

As task history grows, replace this with targeted database queries,
projections, and/or pagination so completed and archived task history does
not need to be loaded for every task-list request.

Revisit when:
- task volume begins affecting task-list latency or memory usage;
- pagination is introduced;
- completed/archived history becomes large;
- database-side filtering is otherwise required.



1. UserSettings current-user guard — KEEP

M-001 is valid.

TaskService establishes the stronger convention:

IsAuthenticated == true
UserId != Guid.Empty

whereas UserSettingsService apparently uses UserId directly. That means the two application services disagree about what constitutes a valid current user.

This matters because TaskService itself depends on:

IUserSettingsService

for timezone-aware behavior.

I would fix this now, while the application is small.

Pre-Milestone Ticket A — Standardize current-user validation

Goal: Make UserSettingsService enforce the same current-user contract as TaskService.

Likely files:

src/LifeOS.Infrastructure/Services/UserSettingsService.cs
tests/LifeOS.Tests/Services/UserSettingsServiceTests.cs

Expected behavior:

Unauthenticated
        ↓
CurrentUserUnavailableException

Guid.Empty
        ↓
CurrentUserUnavailableException

No new abstraction is necessary. I would not create some CurrentUserValidator, base service, middleware, etc. Two services do not justify that yet.

Priority: Do before next milestone.

2. UserSettings soft-delete contradiction — RESOLVED — Milestone 4 Ticket 2

M-002 is the most important finding in the report.

The current combination is:

UserSettings.UserId
        ↓
UNIQUE INDEX

+

UserSettings : BaseEntity
        ↓
soft deletion

+

GetCurrentUserSettingsAsync()
        ↓
can't find deleted settings
        ↓
creates defaults

That produces the deterministic failure Copilot identified:

soft-delete UserSettings
        ↓
global filter hides row
        ↓
service thinks settings don't exist
        ↓
INSERT new UserSettings with same UserId
        ↓
UNIQUE constraint violation

The audit correctly says we first need to decide what the lifecycle means rather than blindly changing the index.

My recommendation

UserSettings should not be independently deletable.

Conceptually, there should be exactly one settings record for a user. Settings aren't historical business records like Tasks.

Therefore I would not solve this with a partial unique index allowing multiple deleted settings rows.

Instead, establish:

UserSettings has no independent delete lifecycle. It exists for the lifetime of the user. Future user-account deletion can deal with settings as part of that account lifecycle.

That preserves:

UNIQUE(UserId)

which is exactly the invariant we actually want.

There is currently no DeleteUserSettingsAsync, so we're already close to that model. The integration test that manually deletes settings was useful for proving the generic soft-delete infrastructure, but it accidentally demonstrated a lifecycle that the application doesn't actually support.

Pre-Milestone Ticket B — Define UserSettings lifecycle

This ticket established:

document that UserSettings cannot be independently deleted;
preserve the unique UserId index;
ensure application APIs expose no settings deletion;
replace the integration test that implied independent deletion with PostgreSQL uniqueness coverage;
add model coverage protecting the one-settings-per-user invariant.

Do not remove BaseEntity inheritance just to solve this. That would be a larger architecture change than necessary.

Priority: Do before next milestone.

3. Misleading UI error after successful action — RESOLVED — Milestone 4 Ticket 3

L-001 is real.

Right now:

Complete Task succeeds
       ↓
database committed
       ↓
RefreshTasksAsync fails
       ↓
catch Exception
       ↓
"Something went wrong while completing the task"

The Task actually was completed.

That's misleading.

It's not important enough to delay the next milestone, but I'd record it because the same pattern could get copied into future vertical slices.

TD candidate — Separate mutation failures from refresh failures
Status: Open
Priority: Low
Introduced: Milestone 3

Task and Dashboard lifecycle handlers currently execute the persisted
mutation and subsequent UI refresh inside the same error boundary.

If persistence succeeds but the refresh fails, the UI can incorrectly
report that the lifecycle operation itself failed.

Future UI action handling should distinguish mutation failure from
post-success refresh failure so persisted success is represented accurately.

This becomes more valuable once Habits/Finance/etc. start implementing similar UI mutations.

4. Non-collapsible headers are buttons — RESOLVED — Milestone 4 Ticket 4

L-002 is also legitimate accessibility feedback.

If:

Overdue
Today
Upcoming
Unscheduled

cannot collapse, their headers shouldn't be <button> elements that do nothing.

Ticket 4 separates the semantic heading used by fixed sections from the
interactive button used by collapsible sections:

Collapsible section
    → button + aria-expanded

Fixed section
    → heading/non-interactive header

Priority: Fix when convenient. Doesn't block anything.










































## TD-003 — Global soft-delete query filter
**Status: Completed — Milestone 3**

Originally, soft-delete filtering was configured specifically for UserSettings.
As part of the Tasks vertical slice, AppDbContext was updated to apply the
IsDeleted == false query filter automatically to all mapped BaseEntity types.