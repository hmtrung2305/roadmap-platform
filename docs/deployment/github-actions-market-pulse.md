# Market Pulse with GitHub Actions

This guide runs the scheduled Job Market refresh with GitHub Actions. The workflow uses the .NET `RoadmapPlatform.MarketPulseJob` project and the configured Jobs API source.

## Goal

The Web API serves Job Market overview data from the persisted database snapshot. A separate scheduled job pulls from Jobs API and persists the daily snapshot into Supabase/PostgreSQL for lifecycle and historical analysis.

Flow:

```text
GitHub Actions schedule / manual trigger
  -> dotnet run RoadmapPlatform.MarketPulseJob
  -> JobsApi source adapter
  -> Upsert Supabase/PostgreSQL current + analytical tables
  -> Web API reads DB snapshot with a short overview cache
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

## Step 3: Jobs API Repository Variables

Create variables under:

```text
Settings -> Secrets and variables -> Actions -> Variables
```

Required variable:

```text
MARKET_PULSE_JOBS_API_BASE_URL
```

Example value:

```text
MARKET_PULSE_JOBS_API_BASE_URL=https://<jobs-api-domain>
```

The workflow derives these URLs from the base URL when they are not set:

```text
MARKET_PULSE_ACTIVE_JOBS_API_URL=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MARKET_PULSE_TODAY_JOBS_API_URL=https://<jobs-api-domain>/api/v1/jobs/today
```

You can still define either URL explicitly as a repository variable when you
need a custom query. Manual workflow runs can also override the base URL or the
full active/today URLs. Repository variables are literal values. Do not use
Windows CMD interpolation such as `%jobsApi%` here; paste the full production
URL into each variable.

Optional variables:

```text
MARKET_PULSE_ACTIVE_JOBS_API_URL
MARKET_PULSE_TODAY_JOBS_API_URL
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
Market Pulse result: snapshotDate=..., sources=..., scraped=..., inserted=..., updated=..., seen=..., expiredInRun=...
Market Pulse cron refresh finished successfully...
```

Then open the app's Market Pulse page or call:

```http
GET /api/market-pulse/overview
GET /api/market-pulse/overview?days=14&skills=react&location=Ha%20Noi
```

If using the internal Web API endpoint instead of the `.NET MarketPulseJob`, set
`MarketPulse:InternalApiKey` as a secret/env value and call:

```http
POST /api/internal/market-pulse/refresh
X-Market-Pulse-Key: <strong-internal-key-at-least-16-chars>
```

## Operational Notes

- Scheduled workflows only run when the workflow file is on the default branch.
- GitHub may delay scheduled workflows during high load.
- Public repositories can use standard GitHub-hosted runners without extra setup.
- Use a stable Jobs API domain for production. Keep one-off tunnel URLs for local demos only.
- `MaxPostingsPerSource` caps how many Jobs API postings are persisted by the scheduled refresh.
- Prefer `MARKET_PULSE_ACTIVE_JOBS_API_URL=https://<crawler-host>/api/v1/jobs?active=true&sort=post_date_desc` so the adapter uses the versioned envelope contract and pagination.

## CI, Security, And Health Checks

The repository also includes `.github/workflows/ci.yml` for pull requests and
pushes to `main`:

- .NET restore/build, plus `dotnet test` when test projects exist.
- Frontend `npm ci`, `npm run lint`, and `npm run build`.
- Guardrails for committed runtime files, hardcoded ngrok hosts,
  `UserSecretsId`, and weak sample secrets in project/config files.

Runtime health endpoints:

```http
GET /health
GET /ready
```

Use `/health` for lightweight process checks and `/ready` for deployment
readiness because it verifies database connectivity. Keep the protected
`GET /api/Home/check-connection` endpoint for authenticated admin diagnostics.

`ConnectionStrings:DefaultConnection` must be supplied by environment variable,
deployment secret, or a local secret. Do not commit database credentials in
`appsettings.json`.

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
- If the Jobs API is deployed with Docker Compose, make sure the `jobs-scheduler`
  profile is running or run a one-shot `jobs-crawler-worker`. The web service
  alone intentionally does not crawl in production.
- If the Jobs API uses SQLite on a hosted platform, mount a persistent disk for
  `/app/data` or switch `DATABASE_URL` to durable storage. Ephemeral disks become
  empty after redeploy/restart.
- Check workflow logs for HTTP status warnings from `JobsApiClient`.
