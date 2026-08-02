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

As more modules are introduced (Tasks, Habits, Finance, Calendar, Reminders, XP), extract these responsibilities into dedicated persistence behaviors (or EF Core interceptors if appropriate).

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

After Milestone 3.

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

Early Milestone 3.

---

## TD-003 — Global soft delete convention

### Current implementation

Soft delete query filters are configured individually.

Currently only `UserSettings` requires one.

### Future improvement

When the second soft-deletable entity (`TaskItem`) is introduced, replace per-entity filters with a reusable convention that automatically applies:

```csharp
HasQueryFilter(e => !e.IsDeleted)
```

to every entity deriving from `BaseEntity`.

### Benefits

- Eliminates duplicated configuration.
- Prevents developers forgetting query filters.
- Keeps soft delete consistent across all modules.

### Priority

Medium

### Target

Beginning of Milestone 3.

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