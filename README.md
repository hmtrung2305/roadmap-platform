# Roadmap Platform Docs Guide

This README is a quick map for the project documentation.

Use it to find the right guide, API endpoint docs, database scripts, and seed files.

## General Docs

| Document | Purpose |
|---|---|
| `docs/project-structure.md` | Explains the project structure, backend layers, and dependency direction. |
| `docs/project-workflow.md` | Explains the development workflow and backend setup notes. |
| `docs/database-workflow.md` | Explains the database workflow for schema changes, migrations, seeds, resets, and scaffolding. |
| `docs/git-workflow.md` | Explains branch naming, commits, pull requests, and team Git workflow. |
| `docs/naming-conventions.md` | Defines naming rules used across the project. |
| `docs/captcha.md` | Explains CAPTCHA setup and integration behavior. |
| `docs/market-pulse-topcv.md` | Explains the Market Pulse Jobs API pipeline, configuration, and smoke tests. |
| `docs/market-pulse/market-pulse-architecture.md` | Defines Python, .NET, database, workflow, and frontend ownership boundaries. |
| `docs/market-pulse/data-contract-python-to-dotnet.md` | Defines Jobs API, relative-date evidence, health, and import completeness contracts. |
| `docs/market-pulse/operations-runbook.md` | Provides production setup, migration, health, refresh, troubleshooting, and acceptance procedures. |
| `docs/market-pulse-phase12-ingestion.md` | Explains the phase 12 paginated Jobs API ingestion path from crawler API into Market Pulse. |
| `docs/supabase-storage.md` | Explains Supabase storage setup for uploaded resources. |
| `docs/ai-credit-limits.md` | Explains AI credit limit rules and implementation behavior. |

## API Docs

| Folder | Purpose |
|---|---|
| `docs/api/` | Contains API endpoint documentation grouped by feature. |

API docs should focus on endpoint contracts, including routes, methods, request bodies, responses, rules, and caveats.

## Database Docs

| File | Purpose |
|---|---|
| `docs/database/schema.sql` | Full database schema snapshot. Use this to understand the current database structure. |
| `docs/database/reset-database.sql` | Database reset script. Use this when a clean database state is needed. |
| `docs/database/seed.sql` | Main seed runner for `psql`. This file can include other seed files and should not be pasted directly into Supabase SQL Editor unless converted to plain SQL. |

## Database Migrations

| Folder | Purpose |
|---|---|
| `docs/database/migrations/` | Holds ordered database migration scripts. Each migration should represent a clear schema change. |

## Database Seeds

| Folder Pattern | Purpose |
|---|---|
| `docs/database/seeds/core/` | Holds shared seed data used across multiple features, such as shared skills. |
| `docs/database/seeds/<feature>/*.seed.sql` | Holds feature-specific seed files. For example, roadmap seeds live under `docs/database/seeds/roadmaps/`. |

## Notes

- For detailed implementation explanations view the matching file in `docs/`.
- Put endpoint-specific documentation inside `docs/api/`.
- Put schema, migration, reset, and seed workflow details inside the database docs.
- Update this README only when documentation structure changes.

## Summary

This README is a documentation map. Use `docs/` for guides, `docs/api/` for endpoint docs, and `docs/database/` for schema, migration, reset, and seed files.
