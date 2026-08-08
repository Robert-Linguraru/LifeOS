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

TD-003 — Global soft-delete query filter
Status: Completed — Milestone 3

Originally, soft-delete filtering was configured specifically for UserSettings.
As part of the Tasks vertical slice, AppDbContext was updated to apply the
IsDeleted == false query filter automatically to all mapped BaseEntity types.

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