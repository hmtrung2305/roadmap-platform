# Market Pulse Operations Runbook

## Required secrets

- Python `.env`: `ADMIN_API_KEY` (at least 16 private characters).
- .NET/GitHub: `MARKET_PULSE_JOBS_API_KEY`, equal to Python `ADMIN_API_KEY`.
- .NET/GitHub: `MARKET_PULSE_DB_CONNECTION_STRING` or local `ConnectionStrings:DefaultConnection`.
- Optional internal automation: `MarketPulse:InternalApiKey` for `/api/internal/market-pulse/*`.

URLs, timeouts, freshness limits, and page limits are configuration variables, not secrets.

## Normal startup

Python API plus scheduled crawling:

```powershell
cd C:\Users\Admin\Documents\Codex\2026-07-14\proi\work\api-job-market-main
docker compose --profile scheduler up -d --build jobs-api-web jobs-scheduler
docker compose ps
```

After Python-only code changes, rebuild the affected service. Use `docker compose up -d --build jobs-api-web` for API/contract changes; rebuild `jobs-scheduler` as well for crawler/model/database changes.

.NET and frontend:

```powershell
cd C:\Users\Admin\Documents\Codex\2026-07-14\proi\work\roadmap-platform-docs-backend-code-documentation
dotnet run --project "src/backend/RoadmapPlatform.Api/RoadmapPlatform.Api.csproj" --launch-profile https
```

```powershell
cd C:\Users\Admin\Documents\Codex\2026-07-14\proi\work\roadmap-platform-docs-backend-code-documentation\src\frontend
npm.cmd run dev
```

## Migrations

For an installation upgraded from before Option A, apply `037` through `042` in numeric order. Migration `024` is also required for admin operations tables.

```powershell
psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 -f "database/migrations/042-market-pulse-relative-date-observations.sql"
```

Always point `DATABASE_URL` at the same PostgreSQL database used by `RoadmapPlatform.Api`.

## Health checks

```powershell
Invoke-RestMethod "http://localhost:8000/health"
$headers = @{ "X-API-Key" = "<ADMIN_API_KEY>" }
Invoke-RestMethod "http://localhost:8000/api/v1/ops/health-summary" -Headers $headers
Invoke-RestMethod "http://localhost:8000/api/v1/jobs?page=1&pageSize=10&active=true"
```

On the admin page, confirm Python crawler health and .NET import health independently. A Python error should not crash or blank the rest of the page.

## Manual refresh

Use the admin **Manual Refresh** button or run:

```powershell
dotnet run --project "src/backend/RoadmapPlatform.MarketPulseJob/RoadmapPlatform.MarketPulseJob.csproj"
```

HTTP `409 Conflict` means a refresh is already running; wait for that run to finish. HTTP `500/503` requires checking backend logs and migration state. UI error payloads are normalized to text and must not be rendered as raw React children.

## Troubleshooting

- `unauthorized`: `MarketPulse__JobsApiKey` does not equal Python `ADMIN_API_KEY`.
- `not_configured`: set `MarketPulse__JobsApiOpsHealthUrl` or a derivable Jobs API URL.
- `stale`: inspect scheduler logs and the latest listing crawl before importing.
- `blocked`/`layout_changed`: inspect Python debug HTML and source selectors; do not retry through the .NET failure queue.
- partial sync: raise `MaxPostingsPerSource`/page limits or repair pagination; missing lifecycle remains intentionally skipped.
- empty source: the workflow blocks when Python reports zero active jobs, and `MarketPulseJob` exits `2` when no acceptable fresh records were imported.
- admin endpoints fail with schema missing: apply `024` and `037` through `042` to the backend database.

Logs:

```powershell
docker compose logs --tail 200 jobs-api-web jobs-scheduler
```

## Deployment acceptance

1. Python readiness and protected ops health pass.
2. Latest listing crawl is within freshness policy and not critical.
3. Migration `042` exists in PostgreSQL.
4. A full .NET import exits zero and reports lifecycle state.
5. Admin page shows both health domains without console rendering errors.
6. A sample relative job contains representative date, bounds, confidence, and observation date.
7. Backend tests, Python tests/lint, and frontend lint/build pass.
