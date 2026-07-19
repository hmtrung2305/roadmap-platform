# Market Pulse TopCV Operations Runbook

## Secrets and configuration

Required secrets:

- Python: `ADMIN_API_KEY`, at least 16 private characters.
- .NET: `MarketPulse:JobsApiKey`, exactly equal to Python `ADMIN_API_KEY`.
- .NET: `ConnectionStrings:DefaultConnection`.
- Optional internal automation: `MarketPulse:InternalApiKey`.

URLs, page limits, freshness thresholds, timeouts, lifecycle settings, and timezone are configuration, not secrets.

Local .NET user-secrets:

```powershell
Set-Location ".\src\backend\RoadmapPlatform.Api"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Port=5432;Database=roadmap;Username=postgres;Password=<password>"

dotnet user-secrets set "MarketPulse:JobsApiUrl" `
  "http://localhost:8000/api/v1/jobs"

dotnet user-secrets set "MarketPulse:JobsApiOpsHealthUrl" `
  "http://localhost:8000/api/v1/ops/health-summary"

dotnet user-secrets set "MarketPulse:JobsApiCrawlTriggerUrl" `
  "http://localhost:8000/api/crawl/listing/run"

dotnet user-secrets set "MarketPulse:JobsApiCrawlStatusUrl" `
  "http://localhost:8000/api/v1/crawl-runs/latest?pipeline=listing&limit=1"

dotnet user-secrets set "MarketPulse:JobsApiKey" "<same ADMIN_API_KEY>"
```

Recommended non-secret settings:

```text
MarketPulse__JobsApiPageSize=100
MarketPulse__JobsApiMaxPages=500
MarketPulse__JobsApiMaxItems=50000
MarketPulse__JobsApiMaxFreshnessHours=24
MarketPulse__RequestTimeoutSeconds=30
MarketPulse__HistoryLookbackDays=400
MarketPulse__RefreshOperationTimeoutMinutes=30
MarketPulse__BusinessTimezone=Asia/Ho_Chi_Minh
```

There is no `MarketPulse:Sources` configuration.

## Safe upgrade order

### 1. Stop Python roles and back up SQLite

```powershell
Set-Location "D:\demo\craw"
docker compose --profile scheduler down

Copy-Item ".\data\topcv_jobs.db" `
  ".\data\topcv_jobs.pre-topcv-refactor.db"
```

Keep the backup until both migrations, backfill, historical sync, and acceptance checks succeed.

### 2. Migrate the Python store

Always run dry-run first. A non-TopCV row is a hard stop and must be investigated instead of deleted automatically.

```powershell
docker compose run --rm jobs-api-web `
  python -m scripts.migrate_topcv_only --dry-run

docker compose run --rm jobs-api-web `
  python -m scripts.migrate_topcv_only --apply
```

Expected result: retained row counts match preflight, the obsolete analytics tables/source columns are absent, foreign-key verification is clean, and `integrity_check=ok`.

### 3. Backfill retained TopCV date evidence

```powershell
docker compose run --rm jobs-api-web `
  python -m crawler.post_date_backfill --dry-run

docker compose run --rm jobs-api-web `
  python -m crawler.post_date_backfill --apply
```

Run apply a second time as an idempotency check. Expected `updated=0` on the second run. Unknown/unparseable text remains unknown and is reported; exact/relative rows are not overwritten.

### 4. Back up PostgreSQL and run consolidated 039

```powershell
Set-Location "D:\demo\roadmap"

pg_dump "$env:DATABASE_URL" -Fc `
  -f ".\market-pulse-pre-topcv-refactor.dump"

psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 `
  -f ".\database\migrations\039-market-pulse-topcv-consolidated.sql"
```

Run this consolidated file once even if the database already ran an older 039. It is idempotent but migration-file renaming does not cause external migration runners to replay it automatically. A detected non-TopCV source aborts the transaction without partial schema changes.

### 5. Test and build

Roadmap backend:

```powershell
dotnet test ".\tests\backend\RoadmapPlatform.Tests\RoadmapPlatform.Tests.csproj"
dotnet build ".\src\backend\RoadmapPlatform.Api\RoadmapPlatform.Api.csproj" -c Release
```

Python:

```powershell
Set-Location "D:\demo\craw"
docker compose run --rm jobs-api-web pytest -q
```

Frontend:

```powershell
Set-Location "D:\demo\roadmap\src\frontend"
npm.cmd ci
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

### 6. Start Python and establish a proven complete crawl

```powershell
Set-Location "D:\demo\craw"
docker compose up -d --build jobs-api-web
docker compose --profile scheduler up -d --build jobs-scheduler
```

The migration marks legacy crawl successes as incomplete because older capped runs cannot prove full coverage. Complete one fresh listing scan from page 1 through the end before importing. The default cap is 200; raise it up to 500 if TopCV still has another non-empty page at the cap.

```powershell
$headers = @{ "X-API-Key" = "<ADMIN_API_KEY>" }

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8000/api/crawl/listing/run?max_pages=200" `
  -Headers $headers

Invoke-RestMethod `
  -Uri "http://localhost:8000/api/v1/crawl-runs/latest?pipeline=listing&limit=1" `
  -Headers $headers
```

Do not continue until the latest listing row reports `status=success` and `is_complete=true`. `partial` means the cap, resume suffix, lease, blocking, or layout condition did not prove a full dataset; it is safe because no missing-job deactivation or freshness publication occurs.

### 7. Synchronize publication history

```powershell
Set-Location "D:\demo\roadmap"

dotnet run `
  --project ".\src\backend\RoadmapPlatform.MarketPulseJob\RoadmapPlatform.MarketPulseJob.csproj" `
  -- --mode history-sync --lookback-days 400
```

History sync must finish every `scope=all` page before it advances `market_pulse_publication_history_state`. After the newly proven complete crawl gates freshness, the start watermark is the earliest retained Python job `first_seen_at`, clamped to the requested 400-day window. A failed run is safe to rerun.

### 8. Run the application

Backend:

```powershell
Set-Location "D:\demo\roadmap"
dotnet run `
  --project ".\src\backend\RoadmapPlatform.Api\RoadmapPlatform.Api.csproj" `
  --launch-profile https
```

Frontend:

```powershell
Set-Location "D:\demo\roadmap\src\frontend"
npm.cmd run dev
```

Open `/market-pulse` for public analytics and `/admin/market-pulse` for the Operations Console.

## Day-to-day operation

After Python API-only changes:

```powershell
docker compose up -d --build jobs-api-web
```

After crawler, parser, model, or database changes, rebuild both long-running roles:

```powershell
docker compose up -d --build jobs-api-web
docker compose --profile scheduler up -d --build jobs-scheduler
```

The scheduler runs crawls on its configured schedule. Rebuilding `jobs-api-web` does not itself mean a new listing crawl has completed.

Use **Refresh TopCV market data** for an end-to-end operation. It persists `queued -> crawling -> importing -> success/failed`; the UI can be reloaded safely. A 409 response means another operation owns the pipeline lock and includes that operation for continued polling.

Use **Import latest crawler data** only when a fresh completed Python crawl already exists. Use **Historical sync** after date backfill, retention recovery, or when the history watermark is incomplete.

The public **Reload market insights** button only repeats `GET /api/market-pulse/overview`; it never triggers crawler work.

## Health and smoke checks

Python:

```powershell
Invoke-RestMethod "http://localhost:8000/health"
$headers = @{ "X-API-Key" = "<ADMIN_API_KEY>" }
Invoke-RestMethod "http://localhost:8000/api/v1/ops/health-summary" -Headers $headers
Invoke-RestMethod "http://localhost:8000/api/v1/jobs?page=1&pageSize=10&scope=all&source=topcv"
```

Compatibility check: `source=topcv` succeeds; `source=another-provider` returns 400.

.NET:

```powershell
Invoke-RestMethod "https://localhost:7103/api/market-pulse/overview?days=30"
Invoke-RestMethod "https://localhost:7103/api/market-pulse/admin/dashboard" -Headers $adminHeaders
```

The overview should expose `publicationAnalytics.basis=published_date`, `dateModel=interval_weighted`, exact/relative totals, coverage watermark, and null/retired observation analytics.

## Database verification

```sql
SELECT external_id, COUNT(*)
FROM public.job_posting
GROUP BY external_id
HAVING COUNT(*) > 1;

SELECT *
FROM public.market_pulse_publication_history_state;

SELECT status, COUNT(*)
FROM public.market_pulse_refresh_operation
GROUP BY status;
```

Expected:

- no duplicate `external_id`;
- one history-state row;
- at most one refresh operation in `queued`, `crawling`, or `importing`;
- no `job_portal_source`, `market_pulse_source_health`, or daily observation tables.

## Troubleshooting

- `post_date_confidence is null`: Python backfill repairs Python rows; consolidated 039 repairs/validates imported PostgreSQL values. Verify both were run against the databases actually used by the processes.
- Trend chart says history is unavailable: run backfill, a complete `scope=all` history sync, then reload the overview. Active jobs alone are not proof of historical publication coverage.
- Trend values contain `~`: at least one TopCV relative week/month/day estimate contributes. The tooltip and table show exact and estimated components.
- Previous period shows `insufficient`: its entire window is not inside the history watermark. Do not interpret it as zero.
- Refresh remains `crawling`: inspect Python listing run status, blocked pages, scheduler/web shared volume, and the configured operation timeout.
- Refresh fails before import: partial, blocked, stale, failed, or non-new crawler results are intentionally rejected.
- Import lag is high: verify `JobsApiUrl`, API key, pagination/max items, network timeout, and PostgreSQL advisory-lock holder.
- HTTP 409: poll the operation returned by the response; do not start a second coordinator.
- React “Objects are not valid as a React child”: error handling must pass the API error `message` string, not the structured `{ code, message, details }` object.

Logs:

```powershell
docker compose logs --tail 200 jobs-api-web jobs-scheduler
```

## Rollback

Stop both Python roles before restoring SQLite. Stop .NET API/job processes before restoring PostgreSQL. Restore both backups from the same rollout boundary; do not combine a pre-refactor store with a post-refactor service.

```powershell
docker compose --profile scheduler down
Copy-Item ".\data\topcv_jobs.pre-topcv-refactor.db" ".\data\topcv_jobs.db" -Force
```

Use `pg_restore` with the deployment's normal database-replacement procedure for the PostgreSQL custom-format dump.
