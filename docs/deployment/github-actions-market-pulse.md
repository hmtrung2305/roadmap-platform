# Market Pulse TopCV import with GitHub Actions

`.github/workflows/market-pulse-refresh.yml` runs the standalone .NET import job after validating the Python TopCV crawler's readiness and freshness. The public Web API remains DB-first and does not crawl during user requests.

```text
GitHub schedule/manual dispatch
  -> Python readiness and protected crawler-health checks
  -> RoadmapPlatform.MarketPulseJob
  -> TopCvJobsApiClient
  -> PostgreSQL canonical jobs/import state
  -> publication-date overview served by RoadmapPlatform.Api
```

## Schedule

The default `0 22 * * *` cron is 05:00 Vietnam time on the following day. GitHub schedules use UTC and can be delayed under load.

## Required secrets

```text
MARKET_PULSE_DB_CONNECTION_STRING
MARKET_PULSE_JOBS_API_KEY
```

The API key must equal Python `ADMIN_API_KEY`. Never commit either value.

## Repository variables

Required:

```text
MARKET_PULSE_JOBS_API_URL=https://<jobs-api-domain>/api/v1/jobs
```

Optional:

```text
MARKET_PULSE_JOBS_API_OPS_HEALTH_URL=https://<jobs-api-domain>/api/v1/ops/health-summary
MARKET_PULSE_JOBS_API_HEALTH_TIMEOUT_SECONDS=10
MARKET_PULSE_JOBS_API_MAX_FRESHNESS_HOURS=24
MARKET_PULSE_JOBS_API_PAGE_SIZE=100
MARKET_PULSE_JOBS_API_MAX_PAGES=50
MARKET_PULSE_JOBS_API_MAX_ITEMS=5000
MARKET_PULSE_REQUEST_TIMEOUT_SECONDS=30
```

The workflow can derive readiness and ops-health URLs from the full jobs URL. Repository variables are literal; do not use Windows `%variable%` syntax.

There are no `Sources__*`, HTML scraper, per-source, or `MaxPostingsPerSource` settings.

## Database prerequisite

Back up PostgreSQL and run:

```powershell
psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 `
  -f ".\database\migrations\039-market-pulse-topcv-consolidated.sql"
```

Run the consolidated file once even if an older 039 is present. The import job expects the renamed import tables, global external-ID uniqueness, refresh-operation state, and publication-history state.

## Manual test

Open **Actions -> Refresh Market Pulse -> Run workflow**. The optional input `jobs_api_url` overrides the full TopCV Jobs API URL; `max_postings` overrides `JobsApiMaxItems`.

Successful logs show:

```text
Python Jobs API readiness check passed.
Crawler pipeline status: healthy
Crawler freshness age: ...
Market Pulse cron refresh finished successfully.
```

A deliberately low maximum can make the import partial. Partial imports never run missing-item lifecycle and should not be used as a production refresh.

## Verification

```http
GET /api/market-pulse/overview?days=30
GET /api/market-pulse/admin/dashboard
```

The overview must return `publicationAnalytics` anchored to the latest successful TopCV crawl. A scheduled import alone does not replace the one-time `--mode history-sync --lookback-days 400` required after rollout.

## Debugging

- Missing DB secret: set `MARKET_PULSE_DB_CONNECTION_STRING`.
- Unauthorized health/import: `MARKET_PULSE_JOBS_API_KEY` differs from Python `ADMIN_API_KEY`.
- Zero active jobs: verify the Python persistent volume and scheduler; the web role does not crawl on startup.
- Stale/critical/blocked crawler: repair Python before importing. The workflow stops intentionally.
- PostgreSQL relation missing: run consolidated 039 against the same database in the connection secret.
- Partial import: raise `MARKET_PULSE_JOBS_API_MAX_PAGES`/`MAX_ITEMS` or repair pagination.

Use a stable HTTPS Jobs API domain in production. Keep tunnels for local diagnostics only.
