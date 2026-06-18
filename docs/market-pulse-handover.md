# Job Market / Market Pulse Handover

## Muc tieu

Job Market thay phan Market Pulse cu bang nguon Jobs API hien tai:

- Active jobs: `http://localhost:8000/api/jobs?active=true&sort=post_date_desc`
- Jobs posted today: `http://localhost:8000/api/jobs/today`

Chuc nang dap ung:

- FR4.1: Nguon crawl theo lich lay tu Jobs API da duoc scrape tu job portal ben ngoai.
- FR4.2: Backend phan tich tan suat keyword tren cac truong job summary.
- FR4.3: UI hien thi chart tuong tac cho nhu cau skill theo ngay dang tin.

## Kien truc sach sau refactor

```text
Frontend /market-pulse
  -> GET /api/market-pulse/overview?days=14&skills=react&skills=node-js
    -> MarketPulseController
      -> IMarketPulseService
        -> IJobMarketSnapshotProvider
          -> JobsApiClient
            -> /api/jobs?active=true&sort=post_date_desc
            -> /api/jobs/today
        -> JobMarketOverviewBuilder
          -> JobMarketKeywordAnalyzer
          -> MarketPulseOverviewDto
```

Huong phu thuoc:

```text
Api -> Application interfaces
Infrastructure -> Application interfaces/models/services
Application -> pure DTO/model/builder/analyzer, khong phu thuoc HTTP, EF, config
```

Design pattern/ap dung:

- Port and Adapter: `IJobMarketSnapshotProvider` la port, `JobsApiJobMarketSnapshotProvider` va `JobsApiClient` la adapter.
- Builder: `JobMarketOverviewBuilder` gom raw snapshot thanh DTO phuc vu UI.
- Strategy-friendly source model: `JobPortalScraper` van ho tro `Kind = JobsApi` va `Kind = Html`, nen sau nay co the them portal moi ma khong sua controller/UI.
- Pure domain analysis: `JobMarketKeywordAnalyzer` khong dung EF/HTTP, de test rieng.

## Phan da xoa hoac thay the

- Xoa Jobs API client cu trong Infrastructure va thay bang adapter moi gon hon.
- Xoa analyzer cu trong Infrastructure va chuyen keyword analysis ve Application.
- Xoa workflow/runtime phu thuoc backend script noi bo cho Market Pulse.
- Bo branch scraper Python cu trong job market flow.
- Khong con config runtime rieng cho backend script noi bo trong Job Market.

Phan con giu co chu y:

- `MarketPulseService.RefreshAsync` va cac entity DB cu van duoc giu de job theo lich co the persist snapshot vao database.
- HTML scraper fallback van duoc giu nhu mot source adapter tong quat, khong phai backend Python noi bo.
- Khi `ActiveJobsApiUrl` va `TodayJobsApiUrl` duoc cau hinh, overview se doc live Jobs API. Neu live API tra rong do loi tam thoi, service fallback ve DB snapshots cu.
- Khong giu project test rieng trong `src/backend` vi docs cua team dang chuan hoa backend solution quanh `Api`, `Application`, `Infrastructure` va job runner san co.

## Du lieu dau vao

Jobs API response du kien:

```json
{
  "total": 50,
  "data": [
    {
      "id": "2196583",
      "title": "Java Backend Developer",
      "company": "Example Company",
      "category": "Backend",
      "post_date": "2026-06-11",
      "post_date_text": "Today",
      "is_active": true,
      "requirements": [],
      "specialties": [],
      "benefits": [],
      "salary": "20 - 35 trieu",
      "experience": "2 nam",
      "location": "Ha Noi",
      "url": "https://www.topcv.vn/...",
      "updated_at": "2026-06-11T01:04:22.795698"
    }
  ]
}
```

Keyword analyzer doc cac truong:

- `title`
- `company`
- `category`
- `salary`
- `experience`
- `location`
- `requirements`
- `specialties`
- `benefits`

Luu y khach quan: API hien tai khong tra full job description, nen chart la market pulse dua tren summary fields, khong phai full-text market intelligence.

## Database va scaffold

Job Market tiep tuc dung cac table san co de khong pha database cua team:

- `job_portal_source`
- `job_posting`
- `job_posting_daily_snapshot`
- `skill_trend_snapshot`

Migration moi:

```text
database/migrations/010-job-market-jobs-api-fields.sql
```

Migration nay them cac cot typed vao `job_posting`:

- `source_job_id`
- `category`
- `salary`
- `experience`
- `post_date_text`
- `source_updated_at`
- `requirements`
- `specialties`
- `benefits`

`requirements`, `specialties`, `benefits` la `jsonb` array co check constraint. Entity scaffold hien tai map `jsonb` thanh `string`, nen service serialize list thanh JSON string de nhat quan voi cac entity scaffolded khac trong project.

Da cap nhat:

- `docs/database/schema.sql`
- `docs/database/migrations/009-job-market-jobs-api-fields.sql`
- `src/backend/RoadmapPlatform.Infrastructure/Entities/JobPosting.cs`
- `src/backend/RoadmapPlatform.Infrastructure/Data/ApplicationDbContext.cs`

Scaffold lai sau khi apply migration:

```powershell
cd src/backend
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project RoadmapPlatform.Infrastructure `
  --startup-project RoadmapPlatform.Api `
  --context ApplicationDbContext `
  --context-dir Data `
  --output-dir Entities `
  --force `
  --no-onconfiguring
```

Neu scaffold bi loi connection string, dat `ConnectionStrings:DefaultConnection` trong user secrets cua `RoadmapPlatform.Api`, khong commit connection string that.

## API va tham so UI

Endpoint:

```http
GET /api/market-pulse/overview?days=14&skills=react&skills=node-js
```

Query params:

| Param | Kieu | Mac dinh | Rule |
|---|---:|---:|---|
| `days` | number | `30` | Backend clamp trong khoang `7..180`; UI hien co cac nut `7`, `14`, `30`, `60`. |
| `skills` | repeated string | empty | Neu rong, backend chon top skills. Neu co, chart render toi da 6 skill dau tien. |

Response fields chinh:

- `lastUpdatedAt`
- `totalPostings`
- `activePostings`
- `todayPostings`
- `sourceCount`
- `skills`
- `todaySkills`
- `trendPoints`
- `categorySummaries`
- `locationSummaries`
- `todayJobs`
- `recentJobs`

## Cau hinh Job Market

File chinh:

```text
src/backend/RoadmapPlatform.Api/appsettings.json
```

Thong so quan trong:

| Key | Y nghia |
|---|---|
| `MarketPulse:Enabled` | Bat/tat hosted scheduler trong Web API. Nen `false` tren web runtime neu dung GitHub Actions/cron rieng. |
| `MarketPulse:RunOnStartup` | Cho phep refresh ngay khi app start. Nen `false` de tranh cham startup. |
| `MarketPulse:DailyRunTime` | Gio chay scheduler theo config hien tai cua app. |
| `MarketPulse:ActiveJobsApiUrl` | Endpoint active jobs dung cho live overview. Nen dung `/api/jobs?active=true&sort=post_date_desc` de adapter phan trang. |
| `MarketPulse:TodayJobsApiUrl` | Endpoint jobs posted today dung cho live overview. |
| `MarketPulse:JobsApiPageSize` | So job moi page khi goi Jobs API co pagination. |
| `MarketPulse:JobsApiMaxPages` | Gioi han so page toi da moi lan goi live API. |
| `MarketPulse:TrackedKeywords` | Danh sach keyword specs. Dung `|` de khai bao alias, vi du `React|React.js|ReactJS`. |
| `MarketPulse:Sources[].Kind` | `JobsApi` cho source Jobs API, `Html` cho fallback generic HTML scraper. |
| `MarketPulse:Sources[].SearchUrlTemplate` | URL de scheduled refresh doc data source. |
| `MarketPulse:MaxPostingsPerSource` | Gioi han so postings persist khi chay scheduled refresh. |
| `MarketPulse:RequestTimeoutSeconds` | Timeout HTTP client, clamp `5..120` giay. |
| `MarketPulse:ActivePostingLookbackDays` | Lookback khi tinh active postings trong DB fallback. |
| `MarketPulse:MissingScansBeforeStale` | So lan khong thay job truoc khi danh dau stale trong DB lifecycle. |
| `MarketPulse:MinimumPostingsForLifecycleCheck` | Nguong an toan truoc khi mark missing jobs, tranh source fail lam tat ca job bi stale. |

## Cach chay local

Backend:

```powershell
cd src/backend
dotnet run --project RoadmapPlatform.Api/RoadmapPlatform.Api.csproj --urls http://127.0.0.1:5208
```

Frontend:

```powershell
cd src/frontend
npm.cmd ci
$env:VITE_BACKEND_BASE_URL="http://127.0.0.1:5208"
npm.cmd run dev -- --host 127.0.0.1 --port 5173
```

Mo UI:

```text
http://127.0.0.1:5173/market-pulse
```

Smoke test API:

```powershell
Invoke-RestMethod "http://127.0.0.1:5208/api/market-pulse/overview?days=7"
```

## Kiem thu

Backend build:

```powershell
cd src/backend
dotnet build RoadmapPlatform.slnx
```

Frontend build:

```powershell
cd src/frontend
npm.cmd run build
```

Frontend lint cho trang Job Market:

```powershell
cd src/frontend
npx.cmd eslint src/pages/MarketPulsePage.jsx
```

Checklist special cases can kiem tra khi review Job Market:

- Keyword alias case-insensitive.
- Word boundary: `JS` khong match trong `JSON`, `SQL` khong match trong `sqlserver`.
- Alias overlap: `Node.js|Node` khong double count.
- Duplicate keyword specs duoc collapse theo slug.
- Empty snapshot khong tao fake data.
- Dedupe job theo `id`.
- Dedupe job theo `url` khi thieu `id`.
- Filter inactive/unstable jobs.
- Clamp date range toi thieu 7 ngay.
- Unknown selected skill van render zero trend points.
- Gioi han chart toi da 6 selected skills.
- Category/location blank gom vao `Unspecified`.
- DTO trim title, clean duplicate requirements/specialties.
