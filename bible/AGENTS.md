# LifeOS Agent Instructions

LifeOS is a Blazor Server personal life management application built with .NET, EF Core, PostgreSQL, ASP.NET Identity, and Hangfire (or an equivalent background job runner).

The `/docs` folder is the architectural source of truth.

Always consult the relevant documentation before making implementation decisions.

---

# Core Rules

## Scope

- Do not expand V1 scope without explicit approval.
- Do not introduce future modules into V1.
- Do not implement AI features in V1.

## Architecture

- Follow the documented layered architecture.
- Business logic belongs in Application Services.
- Razor components must remain thin.
- Do not inject `AppDbContext` directly into Razor components for feature workflows.
- Use repositories and services according to the documented service boundaries.

## Data

- Every user-owned entity must include `UserId`.
- Use `Guid` identifiers consistently.
- Store timestamps in UTC.
- Use `DateOnly` for calendar dates.
- Use `TimeOnly` for local time-of-day values.
- Store the user's IANA time zone identifier.
- Never use `DateTime.Now` in business logic.
- Follow the documented database constraints and indexing strategy.
- Respect archive and soft-delete cascade behavior defined in the database specification.

## Business Rules

- XP is only awarded through `XPService`.
- Reminder execution must be idempotent.
- Habit completion must prevent duplicate logs.
- Notification delivery follows the documented retry policy.
- Finance calculations must not double-count planned income or allowance.
- Monetary values use `decimal`.

---

# Development Workflow

Before implementing:

1. Read this file.
2. Read the relevant documentation.
3. Produce a short implementation plan for non-trivial work.
4. Keep changes limited to the requested feature.

Before completion:

1. Run `dotnet build`.
2. Run `dotnet test` (when tests exist).
3. Report any failed commands.
4. Summarize modified files.
5. Mention any required manual verification.

---

# Stop Conditions

Pause and request clarification if:

- The request expands the approved V1 scope.
- A database schema change is not described in the documentation.
- The change affects unrelated modules.
- A migration cannot be applied from a clean database.
- Reminder behavior conflicts with the documented timezone strategy.
- Duplicate XP or HabitLog entries could occur.
- A feature bypasses the documented service layer.

---

# Documentation Authority

When implementation conflicts with documentation:

- Follow the documentation.
- Do not invent architecture.
- Do not introduce alternative patterns without approval.

The documentation has completed architecture review and should be treated as the implementation contract.