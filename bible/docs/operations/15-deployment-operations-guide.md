# Deployment & Operations Guide

## Purpose

This document defines how LifeOS is deployed, configured, maintained, and backed up.

It acts as the operational guide for the application throughout its lifecycle.

This document covers:

- Development environment
- Production environment
- Configuration
- Secrets
- Database
- Deployment
- Backups
- Monitoring
- Recovery
- Future infrastructure

---

# Deployment Philosophy

LifeOS is a private application.

The deployment strategy prioritizes:

- Simplicity
- Reliability
- Maintainability
- Low operational cost
- Easy recovery

The deployment process should remain reproducible and fully documented.

---

# Environments

## Development

Purpose

Daily development.

Characteristics

- Local machine
- Visual Studio
- Local PostgreSQL
- Local Hangfire
- User Secrets
- Debug logging

---

## Testing

Purpose

Validation before production.

Characteristics

- Clean database
- Test configuration
- Integration testing
- No production data

---

## Production

Purpose

Daily personal use.

Characteristics

- Release build
- Production PostgreSQL
- Production Hangfire
- HTTPS only
- Automated backups
- Limited logging

---

# Technology Stack

Application

- ASP.NET Core (.NET)
- Blazor Server

Database

- PostgreSQL

Authentication

- ASP.NET Identity

Background Jobs

- Hangfire

Hosting

- Linux VPS (preferred)
- Docker (future)
- Windows Server (supported)

Reverse Proxy

- Nginx

HTTPS

- Let's Encrypt

Remote Access

- Tailscale

---

# Configuration

Configuration files

```
appsettings.json

appsettings.Development.json

appsettings.Production.json
```

Never store secrets inside configuration files.

---

# Secrets

Local Development

Use

```
dotnet user-secrets
```

Store

- Database Connection String
- Admin Password
- API Keys
- Future AI Keys

Never commit secrets to Git.

---

Production

Use

- Environment Variables

or

- Secret Manager

Never hardcode credentials.

---

# Database

Database Engine

```
PostgreSQL
```

Deployment Rules

- Automatic migrations disabled by default
- Manual migration verification before production
- Daily backups
- Restore tested periodically

---

# Database Migration Process

Deployment Order

1. Backup database

2. Apply migrations

3. Verify migration success

4. Verify seed process

5. Start application

6. Smoke test

Rollback

If migration fails

- Restore backup
- Investigate
- Redeploy

---

# Seed Data

Development

Seed

- Development User
- Default Finance Categories
- Initial User Progression

Production

Seed only

- System Categories
- Required Configuration

Never overwrite production data.

---

# Build Process

Release Build

```
dotnet clean

dotnet restore

dotnet build

dotnet test

dotnet publish
```

Publish output becomes deployment artifact.

---

# Deployment Process

Recommended Deployment

```
Pull latest release

↓

Backup Database

↓

Apply Migration

↓

Publish Application

↓

Restart Service

↓

Verify Health

↓

Smoke Test
```

Deployment should always be repeatable.

---

# Hosting Structure

```
LifeOS

Application

↓

Nginx

↓

ASP.NET

↓

Hangfire

↓

PostgreSQL
```

---

# HTTPS

Production requires HTTPS.

Recommended

- Let's Encrypt

Certificates should renew automatically.

---

# Reverse Proxy

Recommended

```
Nginx
```

Responsibilities

- HTTPS
- Reverse Proxy
- Compression
- Static Files
- Security Headers

---

# Tailscale

Remote administration should use Tailscale.

Purpose

- Secure remote access
- No public database exposure
- Private administration

Database should never be directly exposed to the internet.

---

# Background Jobs

Production Jobs

- Reminder Processing
- Future Daily Score
- Future Weekly Review
- Future AI Jobs

Jobs should:

- Log failures
- Retry safely
- Be idempotent

---

# Logging

Development

Verbose logging.

Production

Log

- Startup
- Authentication failures
- Background job failures
- Exceptions
- Migration failures

Do not log

- Passwords
- Tokens
- Journal entries
- Sensitive financial notes
- AI conversations

---

# Monitoring

Monitor

- Application startup
- Database connectivity
- Background jobs
- Disk usage
- Memory usage

Future

- Health endpoint
- Metrics dashboard

---

# Backups

Minimum Policy

Database

- Daily backup

Retention

- 30 Days

Backup verification

- Monthly restore test

Future

- Automated encrypted off-site backup

---

# Recovery

Recovery Process

1. Stop application

2. Restore database

3. Restore application

4. Verify startup

5. Verify data integrity

6. Resume service

---

# Git Strategy

Default Branch

```
main
```

Development

```
feature/*
```

Bug Fixes

```
fix/*
```

Hotfixes

```
hotfix/*
```

Every feature should be merged through Pull Requests.

---

# Versioning

Use Semantic Versioning.

Examples

```
v0.1.0

v0.2.0

v1.0.0
```

Major

Breaking changes.

Minor

New features.

Patch

Bug fixes.

---

# Production Checklist

Before every deployment

- Build succeeds
- Tests pass
- Migrations verified
- Backup completed
- Secrets configured
- Configuration verified
- Release build generated
- Smoke tests completed

---

# Future Improvements

Future deployment enhancements may include

- Docker
- Docker Compose
- GitHub Actions CI/CD
- Automated Testing
- Automatic Deployments
- Monitoring Dashboard
- Health Checks
- Centralized Logging
- Backup Automation

---

# Operational Rules

- Production data is never modified manually.
- Database backups are mandatory before migrations.
- Secrets are never committed.
- All deployments must be repeatable.
- Every deployment must be reversible.
- Infrastructure changes must be documented.
- Application configuration should remain environment-specific.
- Recovery procedures must be tested periodically.