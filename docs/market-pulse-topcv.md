# Market Pulse - Jobs API Pipeline

Market Pulse reads IT hiring signals from the Jobs API and presents them as Job Market demand metrics.

## Sources

- Active jobs: `http://localhost:8000/api/jobs?active=true&sort=post_date_desc`
- Jobs posted today: `http://localhost:8000/api/jobs/today`

## Runtime Flow

```text
Frontend /market-pulse
  -> GET /api/market-pulse/overview
    -> MarketPulseService
      -> IJobMarketSnapshotProvider
        -> JobsApiClient
          -> active jobs endpoint
          -> today jobs endpoint
      -> JobMarketOverviewBuilder
        -> JobMarketKeywordAnalyzer
      -> MarketPulseOverviewDto
```

The scheduled refresh path still uses `IMarketPulseService.RefreshAsync`. With `Sources[].Kind = JobsApi`, it persists the active Jobs API feed into the existing Market Pulse tables.

## Database

The persistent Job Market tables stay compatible with the existing database:

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

## Configuration

`src/backend/RoadmapPlatform.Api/appsettings.json`

```json
{
  "MarketPulse": {
    "Enabled": false,
    "ActiveJobsApiUrl": "http://localhost:8000/api/jobs?active=true&sort=post_date_desc",
    "TodayJobsApiUrl": "http://localhost:8000/api/jobs/today",
    "JobsApiPageSize": 100,
    "JobsApiMaxPages": 10,
    "RequestTimeoutSeconds": 30,
    "MaxPostingsPerSource": 160,
    "TrackedKeywords": [
      "React|React.js|ReactJS",
      "Node.js|NodeJS|Node",
      "Python|Django|FastAPI|Flask"
    ],
    "Sources": [
      {
        "Name": "TopCV Jobs API",
        "Kind": "JobsApi",
        "BaseUrl": "http://localhost:8000",
        "SearchUrlTemplate": "http://localhost:8000/api/jobs?active=true&sort=post_date_desc",
        "Enabled": true,
        "DetailUrlContains": []
      }
    ]
  }
}
```

## Analysis Rules

- Keyword specs support aliases separated by `|`.
- Matching is case-insensitive.
- Matching respects word boundaries.
- Longer aliases are evaluated first to prevent double-counting overlaps such as `Node.js|Node`.
- Duplicate keyword definitions with the same slug are collapsed deterministically.

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
