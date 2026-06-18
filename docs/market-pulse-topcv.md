# Market Pulse - Jobs API Pipeline

Market Pulse reads IT hiring signals from the Jobs API and presents them as Job Market demand metrics in the roadmap platform.

## Source Contract

Prefer the versioned Jobs API contract:

- Active jobs: `https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc`
- Jobs posted today: `https://<jobs-api-domain>/api/v1/jobs/today`
- Job detail: `https://<jobs-api-domain>/api/v1/jobs/{id}`
- Jobs API market overview: `https://<jobs-api-domain>/api/v1/market/overview`
- Jobs API health summary: `https://<jobs-api-domain>/api/v1/ops/health-summary`

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
  -> GET /api/market-pulse/overview
    -> MarketPulseService
      -> IJobMarketSnapshotProvider
        -> JobsApiClient
          -> /api/v1/jobs
          -> /api/v1/jobs/today
      -> JobMarketOverviewBuilder
        -> JobMarketKeywordAnalyzer
      -> MarketPulseOverviewDto
```

The scheduled refresh path still uses `IMarketPulseService.RefreshAsync`. With `Sources[].Kind = JobsApi`, it persists the Jobs API feed into the existing Market Pulse tables.

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

Phase 2 analytical tables are intentionally not included yet. The current Phase 1 contract work keeps storage scope to the existing snapshot tables.

## Deployment Baseline

Production must not depend on `localhost` or hardcoded tunnel URLs.

Backend environment:

```text
Cors__AllowedOrigins__0=https://<frontend-domain>
MarketPulse__Enabled=false
MarketPulse__RunOnStartup=false
MarketPulse__ActiveJobsApiUrl=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MarketPulse__TodayJobsApiUrl=https://<jobs-api-domain>/api/v1/jobs/today
MarketPulse__Sources__0__Name=Jobs API
MarketPulse__Sources__0__Kind=JobsApi
MarketPulse__Sources__0__BaseUrl=https://<jobs-api-domain>
MarketPulse__Sources__0__SearchUrlTemplate=https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc
MarketPulse__Sources__0__Enabled=true
```

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
