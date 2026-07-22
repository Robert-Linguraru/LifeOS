# Dev Workflow

## Purpose

This document defines the development workflow for LifeOS to ensure the project remains maintainable, portable, and under personal ownership while supporting development across multiple devices.

---

# Repository Ownership

The LifeOS repository is owned by the **personal GitHub account**.

The work GitHub account is added as a **collaborator**.

```text
Personal GitHub (Owner)
│
└── LifeOS Repository
      │
      ├── Documentation
      ├── Source Code
      ├── Issues
      ├── GitHub Projects
      ├── GitHub Actions
      └── Releases

Work GitHub (Collaborator)
```

The repository will always remain under the personal account to ensure long-term ownership beyond the internship.

---

# Development Environment

## Personal Laptop

Primary responsibilities:

* Architecture
* Documentation
* Feature planning
* Code reviews
* Personal development
* Release management
* Long-term maintenance

This machine is considered the **primary development environment**.

---

## Work Laptop

Primary responsibilities:

* Daily implementation
* Feature development
* Testing
* Debugging
* GitHub Copilot assisted development

Development is performed using the work GitHub account as a repository collaborator.

---

# Source of Truth

The following order defines the project's source of truth:

1. Documentation (`/docs`)
2. GitHub Issues
3. Source Code

Implementation must follow the documented architecture.

---

# Feature Workflow

Every feature follows the same lifecycle.

```text
Documentation
        │
        ▼
GitHub Issue
        │
        ▼
Feature Branch
        │
        ▼
Implementation
        │
        ▼
Build
        │
        ▼
Tests
        │
        ▼
Manual Verification
        │
        ▼
Pull Request
        │
        ▼
Merge into Main
        │
        ▼
Deploy
```

---

# Branch Strategy

The `main` branch always remains deployable.

Each feature is developed in its own branch.

Example:

```text
main

feature/authentication

feature/tasks

feature/habits

feature/reminders

feature/dashboard

feature/finance
```

---

# Daily Workflow

### Start

* Pull the latest `main`.
* Create a feature branch.
* Review the relevant documentation.
* Create or update the GitHub Issue if necessary.

---

### During Development

* Keep commits focused.
* Commit frequently.
* Push regularly.
* Build often.
* Test continuously.

---

### Before Completion

* Run `dotnet build`.
* Run all available tests.
* Verify the feature manually.
* Open a Pull Request.
* Merge into lete the feature branch.
`main`.
* De
---

# Documentation Workflow

Documentation is considered part of the product.

Documentation should only be updated when:

* Architecture changes.
* Business rules change.
* Database design changes.
* Service responsibilities change.

Implementation details should not trigger documentation updates.

---

# GitHub Usage

GitHub is used for:

* Source Control
* Issues
* Project Planning
* Pull Requests
* Releases
* CI/CD

GitHub Projects should track implementation progress rather than documentation progress.

---

# Deployment Strategy

The application should remain deployable throughout development.

Every completed feature should result in:

* Passing build
* Passing tests
* Successful deployment
* Stable `main` branch

---

# Long-Term Principles

* Keep the architecture simple.
* Follow the documented V1 scope.
* Avoid unnecessary abstractions.
* Build features that solve real personal needs.
* Allow real-world usage to drive future improvements.

LifeOS is a long-term personal product rather than a short-term portfolio project. Development decisions should prioritize maintainability, clarity, and long-term usability over unnecessary complexity.
