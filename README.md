# LifeOS

LifeOS is a personal life management application built with **Blazor Server**, **.NET**, **Entity Framework Core**, **PostgreSQL**, **ASP.NET Identity**, and **Hangfire** (or an equivalent background job runner).

The goal of LifeOS is to provide a fast, structured, and modular personal operating system for managing tasks, habits, reminders, finances, and personal progression.

---

## Documentation First

The `/docs` directory is the **single source of truth** for the project.

All implementation decisions must follow the architecture and specifications documented there.

Before beginning work, read the documentation in the following order:

1. `docs/README.md`
2. `docs/01-v1-scope-prd.md`
3. `docs/03-technical-architecture.md`
4. `docs/04-data-model-database-spec.md`
5. Any additional documents relevant to the feature being implemented.

---

## Technology Stack

- .NET 9
- Blazor Server
- Entity Framework Core
- PostgreSQL
- ASP.NET Identity
- Hangfire (or equivalent scheduler)

---

## Development Principles

- Architecture-first development.
- Services own all business logic.
- Razor components remain thin.
- Database design follows the documented specification.
- Follow the documented V1 scope without expanding features.
- Update documentation only when architectural decisions change.

---

## Current Status

**Phase:** V1 Development

Documentation has completed architecture review and is considered:

**✅ READY TO BUILD**