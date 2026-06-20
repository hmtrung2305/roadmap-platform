# Market Pulse with GitHub Actions

This guide runs the scheduled Job Market refresh with GitHub Actions. The workflow uses the .NET `RoadmapPlatform.MarketPulseJob` project and the configured Jobs API source.

## Goal

The Web API can serve live Job Market overview data from Jobs API. A separate scheduled job can also persist a daily snapshot into Supabase/PostgreSQL for lifecycle and historical analysis.

Flow:

```text
GitHub Actions schedule / manual trigger
  -> dotnet run RoadmapPlatform.MarketPulseJob
  -> JobsApi source adapter
  -> Upsert Supabase/PostgreSQL current + analytical tables
  -> Web API reads live Jobs API or DB fallback
```

No backend script runtime is required for this workflow.

## Main Files

```text
.github/workflows/market-pulse-refresh.yml
src/backend/RoadmapPlatform.MarketPulseJob/
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/JobsApiClient.cs
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/JobPortalScraper.cs
```

## Default Schedule

The workflow uses:

```yaml
schedule:
  - cron: "0 22 * * *"
```

GitHub Actions cron uses UTC. `22:00 UTC` is `05:00` in Vietnam on the next day.

## Step 1: Keep Web API Scheduler Off

In `src/backend/RoadmapPlatform.Api/appsettings.json`, keep:

```json
"MarketPulse": {
  "Enabled": false,
  "RunOnStartup": false
}
```

Reason: the public Web API should respond to users quickly. The scheduled worker should handle persistence.

## Step 2: Add GitHub Secret

Create this repository secret:

```text
MARKET_PULSE_DB_CONNECTION_STRING
```

Example value:

```text
Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;GSS Encryption Mode=Disable
```

Do not commit real connection strings.

Before enabling the workflow, apply database migrations through:

```text
database/migrations/016-market-pulse-analytical-schema.sql
```

The refresh job writes `job_posting`, `job_posting_version`, `job_posting_observation`, `skill_taxonomy`, `job_skill_mention`, `job_market_daily_snapshot`, and `market_pulse_insight_snapshot`.

## Step 3: Required Repository Variables

Create variables under:

```text
Settings -> Secrets and variables -> Actions -> Variables
```

Supported variables:

```text
MARKET_PULSE_ACTIVE_JOBS_API_URL
MARKET_PULSE_TODAY_JOBS_API_URL
MARKET_PULSE_JOBS_API_BASE_URL
```

Example values:

```text
MARKET_PULSE_ACTIVE_JOBS_API_URL=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MARKET_PULSE_TODAY_JOBS_API_URL=https://<jobs-api-domain>/api/v1/jobs/today
MARKET_PULSE_JOBS_API_BASE_URL=https://<jobs-api-domain>
```

The workflow fails clearly when any required URL is missing. It does not fall back to a hardcoded localhost or tunnel URL.

Optional variables:

```text
MARKET_PULSE_MAX_POSTINGS
MARKET_PULSE_JOBS_API_PAGE_SIZE
MARKET_PULSE_JOBS_API_MAX_PAGES
MARKET_PULSE_REQUEST_TIMEOUT_SECONDS
```

## Step 4: Manual Test Run

After the workflow is on the default branch:

```text
GitHub repo -> Actions -> Refresh Market Pulse -> Run workflow
```

Manual input:

```text
max_postings = 80
```

Increase gradually after the job is stable.

## Step 5: Check Result

Successful logs include:

```text
Starting Market Pulse cron refresh...
Market Pulse result: snapshotDate=..., sources=..., scraped=..., saved=..., new=..., updated=...
Market Pulse cron refresh finished successfully...
```

Then open the app's Market Pulse page or call:

```http
GET /api/market-pulse/overview
```

## Operational Notes

- Scheduled workflows only run when the workflow file is on the default branch.
- GitHub may delay scheduled workflows during high load.
- Public repositories can use standard GitHub-hosted runners without extra setup.
- Use a stable Jobs API domain for production. Keep one-off tunnel URLs for local demos only.
- `MaxPostingsPerSource` caps how many Jobs API postings are persisted by the scheduled refresh.
- Prefer `MARKET_PULSE_ACTIVE_JOBS_API_URL=https://<crawler-host>/api/v1/jobs?active=true&sort=post_date_desc` so the adapter uses the versioned envelope contract and pagination.

## Quick Debug

Missing secret:

```text
Missing MARKET_PULSE_DB_CONNECTION_STRING secret.
```

Database error:

```text
NpgsqlException / PostgresException / relation does not exist
```

Check the connection string and verify that the Market Pulse migration has been applied.
For the analytical layer, verify that migration `016-market-pulse-analytical-schema.sql` has also been applied.

Jobs API returns zero jobs:

- Verify `MARKET_PULSE_ACTIVE_JOBS_API_URL`.
- Verify the Jobs API host is reachable from GitHub Actions.
- Check `GET /api/v1/ops/health-summary` on the Jobs API with `X-API-Key`.
- Check `GET /api/v1/crawl-runs/latest?pipeline=listing&limit=10` on the Jobs API with `X-API-Key`.
- Check workflow logs for HTTP status warnings from `JobsApiClient`.
