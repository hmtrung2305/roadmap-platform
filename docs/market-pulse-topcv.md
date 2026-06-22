# Market Pulse - Jobs API Pipeline

Market Pulse reads IT hiring signals from the Jobs API and presents them as Job Market demand metrics in the roadmap platform.

## Source Contract

Prefer the versioned Jobs API contract:

- Active jobs: `https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc`
- Jobs posted today: `https://<jobs-api-domain>/api/v1/jobs/today`
- Job detail: `https://<jobs-api-domain>/api/v1/jobs/{id}`
- Jobs API market overview: `https://<jobs-api-domain>/api/v1/market/overview`
- Jobs API health summary: `https://<jobs-api-domain>/api/v1/ops/health-summary`
- Jobs API crawl run history: `https://<jobs-api-domain>/api/v1/crawl-runs/latest`

List endpoints return a stable envelope:

```json
{
  "ok": true,
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 100,
    "total": 398,
    "totalPages": 4
  },
  "meta": {
    "source": "topcv",
    "generatedAt": "2026-06-18T09:00:00Z",
    "latestSuccessfulCrawlAt": "2026-06-18T08:05:00Z"
  }
}
```

Supported filters on `/api/v1/jobs`:

| Param | Meaning |
|---|---|
| `page`, `pageSize` | Pagination. `pageSize` is capped by the Jobs API. |
| `active` | `true`, `false`, or omitted for both. |
| `skill` | Matches title, requirements, benefits, specialties, and normalized skills. |
| `category`, `city`, `source` | Exact normalized category/source and city/location filters. |
| `salary_min`, `salary_max`, `currency` | Salary range overlap filters. |
| `experience_min`, `experience_max` | Experience range overlap filters. |
| `post_date_from`, `post_date_to` | ISO date range, for example `2026-06-18`. |
| `detail_status` | `pending`, `success`, `failed`, etc. |
| `updated_since` | ISO datetime against `updated_at` or `last_seen_at`. |
| `search` | Title/company search. |
| `sort` | `last_seen_desc`, `last_seen_asc`, `post_date_desc`, `post_date_asc`, `salary_desc`, `salary_asc`. |

Legacy endpoints under `/api/jobs` remain available and are now paginated, but new integrations should use `/api/v1`.

## Runtime Flow

```text
Frontend /market-pulse
  -> GET /api/market-pulse/overview?days=30&skills=react
    -> MarketPulseService
      -> PostgreSQL current + analytical Market Pulse tables
      -> JobMarketOverviewBuilder
        -> JobMarketKeywordAnalyzer
      -> MarketPulseOverviewDto

Refresh / ingest
  -> MarketPulseHostedService, MarketPulseJob, or internal API
    -> JobPortalScraper
      -> JobsApiClient
        -> /api/v1/jobs
    -> Persist lifecycle, observations, skill mentions, daily aggregates
```

The scheduled refresh path still uses `IMarketPulseService.RefreshAsync`. With `Sources[].Kind = JobsApi`, it persists the Jobs API feed into the current Market Pulse tables and the Phase 2 analytical tables.
The public overview path is DB-first and does not call the live Jobs API during a user request.

## Analytics Contract

`GET /api/market-pulse/overview` reads from the persisted DB snapshot and returns
descriptive dashboard fields plus Phase 4 analytical context:

- `insightMeta`: selected period, sample size, confidence, last updated time, and methodology.
- `dataQuality`: score, warning list, coverage rates, source count, freshness hours.
- `insightCards`: compact insight summaries with sample size, period, confidence.
- `risingSkills` / `fallingSkills`: current period versus previous period movement.
- `skillCoOccurrences`: skill pairs that appear together in the same postings.
- `salaryInsight`: salary coverage and parsed monthly VND medians.
- `experienceSummaries`: inferred experience levels.
- `learningRecommendations`: actions for mapping market signals to roadmap/modules.

Supported query parameters:

| Param | Meaning |
|---|---|
| `days` | Analysis window, clamped to 7..180 days. |
| `skills` | Repeatable selected skill slugs, capped at 6. |
| `category`, `location`, `experience`, `source` | Narrow the DB read model before analytics. |
| `salaryMinMonthlyVnd`, `salaryMaxMonthlyVnd` | Salary overlap filter after parsing salary text. |

Every user-facing insight includes enough metadata for the UI to show period,
sample size, and confidence instead of presenting counts as context-free truth.
The overview builder is shared by the scheduled ingest path and DB read model.
Overview responses are cached by query for `MarketPulse:OverviewCacheSeconds`
seconds and invalidated after successful refresh/ingest.

## Frontend Analytics UX

The `/market-pulse` page is a DB-first analytics view over
`GET /api/market-pulse/overview`. Browser requests should point to the roadmap
backend with `VITE_BACKEND_BASE_URL`; the roadmap backend is responsible for
reading the persisted snapshot.

Current UI behavior:

- Window selector: 7, 14, 30, and 90 days.
- Filters sent to backend: `category`, `location`, `experience`, `source`,
  repeatable `skills`, `salaryMinMonthlyVnd`, `salaryMaxMonthlyVnd`.
- Salary inputs are entered as million VND/month in the UI and converted to VND
  before calling the API.
- Insight cards show top rising skill, most demanded role, salary-backed signal,
  data confidence, and what changed since the latest snapshot.
- Data quality states distinguish backend unavailable/permission error, no
  snapshot yet, no matching signal, and stale/low-confidence warnings.
- Skill links open `/learning-modules/browse?q=<skill>`.
- Role/category links open `/roadmaps?q=<role>`; `/roadmaps` reads `?q=` and
  `?role=` query params.
- Job rows open an in-page requirement breakdown drawer using the persisted
  `requirements` and `specialties` fields, plus the source URL.

## Database

The persistent Job Market tables stay compatible with the current database:

- `job_portal_source`
- `job_posting`
- `job_posting_daily_snapshot`
- `skill_trend_snapshot`

`database/migrations/010-job-market-jobs-api-fields.sql` adds typed Jobs API fields to `job_posting` so scaffolded entities and SQL queries do not need to parse `description`:

- `source_job_id`
- `category`
- `salary`
- `experience`
- `post_date_text`
- `source_updated_at`
- `requirements`
- `specialties`
- `benefits`

`database/migrations/016-market-pulse-analytical-schema.sql` adds the analytical layer:

- `job_posting_version` stores one immutable content version per posting/content hash.
- `job_posting_observation` records daily seen/new/updated/stale/expired observations.
- `skill_taxonomy` stores normalized skill names and slugs.
- `job_skill_mention` links postings to normalized skills for aggregate analysis.
- `job_market_daily_snapshot` stores daily aggregate rows by all jobs, category, location, and skill.
- `market_pulse_insight_snapshot` stores generated insight payloads such as the market overview summary.

`database/schema.sql` is updated through migration 016. Apply migrations before running the scheduled refresh against an existing PostgreSQL database.

## Jobs API Worker Reliability

The Python Jobs API should run as separate roles in production:

- `jobs-api-web`: serves FastAPI endpoints only.
- `jobs-scheduler`: runs scheduled listing/detail crawler work.
- `jobs-crawler-worker`: runs one-off listing/detail work for manual recovery.

The web process should keep:

```text
CRAWLER_ENABLED=false
CRAWLER_RUN_IN_WEB_PROCESS=false
CRAWLER_RUN_ON_STARTUP=false
```

The scheduler process should set:

```text
CRAWLER_ENABLED=true
CRAWLER_RUN_ON_STARTUP=false
```

Before investigating missing Market Pulse data, check Jobs API operations:

```text
GET /api/v1/ops/health-summary
GET /api/v1/crawl-runs/latest?pipeline=listing&limit=10
```

Both endpoints require `X-API-Key`.

## Deployment Baseline

Production must not depend on `localhost` or hardcoded tunnel URLs.

Backend environment:

```text
Cors__AllowedOrigins__0=https://<frontend-domain>
MarketPulse__Enabled=false
MarketPulse__RunOnStartup=false
MarketPulse__OverviewCacheSeconds=120
MarketPulse__InternalApiKey=<strong-internal-key-at-least-16-chars>
MarketPulse__ActiveJobsApiUrl=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MarketPulse__TodayJobsApiUrl=https://<jobs-api-domain>/api/v1/jobs/today
MarketPulse__Sources__0__Name=Jobs API
MarketPulse__Sources__0__Kind=JobsApi
MarketPulse__Sources__0__BaseUrl=https://<jobs-api-domain>
MarketPulse__Sources__0__SearchUrlTemplate=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MarketPulse__Sources__0__Enabled=true
```

Render, Railway, GitHub Actions, and most production dashboards do not expand
Windows CMD variables such as `%jobsApi%`. Put the full Jobs API URL in every
environment value there. The `%jobsApi%` examples below are only for a local
Windows CMD session.

Frontend environment:

```text
VITE_BACKEND_BASE_URL=https://<dotnet-api-domain>
```

For local demo without a domain, expose the Jobs API with ngrok and use the generated host as `<jobs-api-domain>`. Do not commit the ngrok URL.

PowerShell:

```powershell
$jobsApi="https://<your-ngrok-host>"
$env:MarketPulse__ActiveJobsApiUrl="$jobsApi/api/v1/jobs?active=true&sort=post_date_desc"
$env:MarketPulse__TodayJobsApiUrl="$jobsApi/api/v1/jobs/today"
$env:MarketPulse__Sources__0__Name="Jobs API"
$env:MarketPulse__Sources__0__Kind="JobsApi"
$env:MarketPulse__Sources__0__BaseUrl=$jobsApi
$env:MarketPulse__Sources__0__SearchUrlTemplate="$jobsApi/api/v1/jobs?active=true&sort=post_date_desc"
$env:MarketPulse__Sources__0__Enabled="true"
$env:MarketPulse__OverviewCacheSeconds="120"
$env:MarketPulse__InternalApiKey="<strong-internal-key-at-least-16-chars>"
```

CMD:

```cmd
set "jobsApi=https://<your-ngrok-host>"
set "MarketPulse__ActiveJobsApiUrl=%jobsApi%/api/v1/jobs?active=true&sort=post_date_desc"
set "MarketPulse__TodayJobsApiUrl=%jobsApi%/api/v1/jobs/today"
set "MarketPulse__Sources__0__Name=Jobs API"
set "MarketPulse__Sources__0__Kind=JobsApi"
set "MarketPulse__Sources__0__BaseUrl=%jobsApi%"
set "MarketPulse__Sources__0__SearchUrlTemplate=%jobsApi%/api/v1/jobs?active=true&sort=post_date_desc"
set "MarketPulse__Sources__0__Enabled=true"
set "MarketPulse__OverviewCacheSeconds=120"
set "MarketPulse__InternalApiKey=<strong-internal-key-at-least-16-chars>"
```

User secrets for local backend:

```powershell
cd src/backend/RoadmapPlatform.Api
dotnet user-secrets set "MarketPulse:ActiveJobsApiUrl" "https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc"
dotnet user-secrets set "MarketPulse:TodayJobsApiUrl" "https://<jobs-api-domain>/api/v1/jobs/today"
dotnet user-secrets set "MarketPulse:Sources:0:Name" "Jobs API"
dotnet user-secrets set "MarketPulse:Sources:0:Kind" "JobsApi"
dotnet user-secrets set "MarketPulse:Sources:0:BaseUrl" "https://<jobs-api-domain>"
dotnet user-secrets set "MarketPulse:Sources:0:SearchUrlTemplate" "https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc"
dotnet user-secrets set "MarketPulse:Sources:0:Enabled" "true"
dotnet user-secrets set "MarketPulse:OverviewCacheSeconds" "120"
dotnet user-secrets set "MarketPulse:InternalApiKey" "<strong-internal-key-at-least-16-chars>"
```

## Smoke Test

Run the backend:

```powershell
cd src/backend
dotnet run --project RoadmapPlatform.Api/RoadmapPlatform.Api.csproj --urls http://127.0.0.1:5208
```

Then call:

```text
http://127.0.0.1:5208/api/market-pulse/overview?days=7
http://127.0.0.1:5208/api/market-pulse/overview?days=14&skills=react&location=Ha%20Noi
```

Internal refresh smoke test:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://127.0.0.1:5208/api/internal/market-pulse/refresh" `
  -Headers @{ "X-Market-Pulse-Key" = "<strong-internal-key-at-least-16-chars>" }
```

Expected high-level fields:

- `activePostings`
- `todayPostings`
- `skills`
- `todaySkills`
- `trendPoints`
- `categorySummaries`
- `locationSummaries`
- `todayJobs`
- `recentJobs`
