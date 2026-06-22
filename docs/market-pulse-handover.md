# Job Market / Market Pulse Handover

## Muc tieu

Job Market thay phan Market Pulse cu bang nguon Jobs API hien tai:

- Active jobs: `https://<jobs-api-domain>/api/v1/jobs?active=true&sort=post_date_desc`
- Jobs posted today: `https://<jobs-api-domain>/api/v1/jobs/today`

Chuc nang dap ung:

- FR4.1: Nguon crawl theo lich lay tu Jobs API da duoc scrape tu job portal ben ngoai.
- FR4.2: Backend phan tich tan suat keyword tren cac truong job summary.
- FR4.3: UI hien thi chart tuong tac cho nhu cau skill theo ngay dang tin.

## Kien truc hien tai

```text
Frontend /market-pulse
  -> GET /api/market-pulse/overview?days=14&skills=react&skills=node-js
    -> MarketPulseController
      -> IMarketPulseService
        -> PostgreSQL current + analytical Market Pulse tables
        -> JobMarketOverviewBuilder
          -> JobMarketKeywordAnalyzer
          -> MarketPulseOverviewDto

Refresh / ingest path
  -> MarketPulseHostedService, MarketPulseJob, or internal endpoint
    -> IMarketPulseService.RefreshAsync / IngestAsync
      -> JobPortalScraper
        -> JobsApiClient
          -> /api/v1/jobs?active=true&sort=post_date_desc
      -> Persist job lifecycle, skill mentions, daily aggregates, insight snapshot
```

Huong phu thuoc:

```text
Api -> Application interfaces
Infrastructure -> Application interfaces/models/services
Application -> pure DTO/model/builder/analyzer, khong phu thuoc HTTP, EF, config
```

Design pattern/ap dung:

- Port and Adapter: refresh/ingest di qua `IJobPortalScraper`, `JobPortalScraper` va `JobsApiClient` adapter de doc Jobs API.
- Builder: `JobMarketOverviewBuilder` gom raw snapshot thanh DTO phuc vu UI.
- Strategy-friendly source model: `JobPortalScraper` van ho tro `Kind = JobsApi` va `Kind = Html`, nen sau nay co the them portal moi ma khong sua controller/UI.
- Pure domain analysis: `JobMarketKeywordAnalyzer` khong dung EF/HTTP, de test rieng.
- DB-first read model: public overview doc tu database/materialized snapshot. Live Jobs API chi dung trong refresh/ingest job de on dinh UX va tranh user request phu thuoc crawler host.

## Phase 0 baseline da ap dung

- Khong de production config tro ve `localhost:8000`.
- Khong hardcode ngrok fallback trong GitHub Actions.
- Frontend yeu cau `VITE_BACKEND_BASE_URL` khi build/deploy va gui header `ngrok-skip-browser-warning` khi backend base URL la ngrok host.
- Jobs API production khong co default admin key yeu, crawler startup duoc tat mac dinh, ops/crawl endpoints duoc bao ve bang `X-API-Key`.
- Runtime SQLite database khong nam trong package source.

Ngrok chi dung cho dev/demo khi chua co domain. Gia tri tunnel phai nam trong env/user-secrets/repository variables, khong commit vao source.

## Phase 1 contract da ap dung

Jobs API co endpoint versioned:

- `GET /api/v1/jobs`
- `GET /api/v1/jobs/active`
- `GET /api/v1/jobs/today`
- `GET /api/v1/jobs/{id}`
- `GET /api/v1/market/overview`
- `GET /api/v1/ops/health-summary`

List response dung envelope:

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

`/api/v1/jobs` ho tro filter:

- `skill`
- `category`
- `city`
- `salary_min`, `salary_max`, `currency`
- `experience_min`, `experience_max`
- `post_date_from`, `post_date_to`
- `detail_status`
- `updated_since`
- `source`
- `active`
- `search`
- `sort`

Endpoint legacy `/api/jobs`, `/api/jobs/active`, `/api/jobs/today` van ton tai nhung da co pagination. Tich hop moi nen dung `/api/v1`.

`.NET JobsApiClient` doc duoc ca envelope moi va response legacy, nen co the rollout Jobs API v1 ma khong lam hong moi truong cu.

## Phase 2 analytical storage da ap dung

Roadmap backend da co analytical schema va pipeline persist them du lieu phan tich khi scheduled refresh chay:

- Moi job co version theo `content_hash` trong `job_posting_version`.
- Moi ngay/source co observation `new`, `seen`, `updated`, `stale_unverified`, `expired` trong `job_posting_observation`.
- Skill normalized duoc upsert vao `skill_taxonomy` va link voi job qua `job_skill_mention`.
- Daily aggregate duoc rebuild vao `job_market_daily_snapshot` theo tong quan, category, location va skill.
- Insight overview duoc luu trong `market_pulse_insight_snapshot` voi payload JSON.

Migration can apply:

```text
database/migrations/016-market-pulse-analytical-schema.sql
```

`database/schema.sql` da duoc cap nhat toi migration 016. Khi them table/cot sau nay, luon them migration tuong ung truoc khi sua schema tong hop.

## Phase 3 crawler reliability da ap dung

Jobs API khong nen chay crawler nang trong web process production. Runtime duoc tach theo vai tro:

- `jobs-api-web`: chi serve FastAPI.
- `jobs-scheduler`: chay scheduled listing/detail.
- `jobs-crawler-worker`: chay one-shot listing/detail khi can recovery.

Run status nen doc theo vocabulary moi:

- `success`
- `partial_success`
- `blocked`
- `layout_changed`
- `empty_protected`
- `failed`

Khi UI Market Pulse khong co du lieu, kiem tra Jobs API truoc:

```http
GET /api/v1/ops/health-summary
GET /api/v1/crawl-runs/latest?pipeline=listing&limit=10
```

Hai endpoint nay can header `X-API-Key`. Neu deploy chi dung GitHub Actions/.NET `MarketPulseJob` de ingest, Web API cua Roadmap van nen giu `MarketPulse:Enabled=false` de tranh co hai scheduler cung ghi DB.
Neu Jobs API chi chay service web production, se khong co crawl moi. Can chay
them scheduler/worker rieng, hoac mot one-shot worker de nap du lieu ban dau.
Neu SQLite chay tren host co filesystem ephemeral, can gan persistent disk vao
`/app/data`; neu khong database se rong sau moi lan redeploy/restart.

Tren Render/Railway/GitHub Actions, gia tri env la literal. Khong dung
`%jobsApi%` trong dashboard production; hay paste full URL vao tung bien.

## Phase 4 analytics layer da ap dung

Market Pulse overview khong chi tra ve job count/keyword count nua. Backend da
bo sung analytics metadata de moi insight co ngu canh:

- `insightMeta`: period, sample size, confidence, last updated, methodology.
- `dataQuality`: score 0-100, salary/category/location/detail coverage,
  freshness, source count, warning list.
- `insightCards`: market size, top rising skill, role demand, location bias,
  salary signal, data confidence.
- `risingSkills` va `fallingSkills`: so sanh window hien tai voi window truoc
  theo `days`.
- `skillCoOccurrences`: cac cap skill hay xuat hien cung nhau trong posting.
- `salaryInsight`: salary coverage, median/min/max monthly VND va breakdown theo
  category.
- `experienceSummaries`: phan bo Intern/Fresher/Junior/Mid/Senior/Lead.
- `learningRecommendations`: goi y action de map skill/skill-pair sang learning
  module hoac project suggestion.

`MarketPulseService.GetOverviewAsync` dung chung `JobMarketOverviewBuilder`
voi refresh/ingest pipeline, nhung public request doc tu DB snapshot. Nho vay UI
nhan cung contract analytics ma khong phu thuoc live crawler host. Khong can
migration moi cho phase nay vi day la lop tinh toan/read model dua tren field
hien co.

Frontend `/market-pulse` da hien thi data confidence banner, insight cards,
rising/falling skills, skill co-occurrence, source/category/location/experience
mix, salary signal va learning actions.

Luu y phan salary chi parse chuoi co so ro rang. Cac job `thoa thuan`,
`negotiable` hoac khong co salary se khong vao median va lam giam coverage.

## Phase 5 backend refactor da ap dung

Public overview da chuyen sang DB-first:

- `GET /api/market-pulse/overview` khong goi live Jobs API trong user request.
- Overview duoc build tu `job_posting` va analytical tables, sau do di qua cung `JobMarketOverviewBuilder` nen van co day du `categorySummaries`, `locationSummaries`, `todayJobs`, `recentJobs`, `todaySkills` va analytics phase 4.
- Live Jobs API chi con nam o ingestion paths: hosted scheduler, `.NET MarketPulseJob`, hoac internal refresh endpoint.
- Overview co cache ngan han `MarketPulse:OverviewCacheSeconds`, mac dinh 120 giay va clamp toi da 300 giay. Cache tu dong invalidate sau refresh/ingest thanh cong.
- Query options moi: `days`, `skills`, `category`, `location`, `experience`, `source`, `salaryMinMonthlyVnd`, `salaryMaxMonthlyVnd`.
- Refresh result tach counter ro rang: `postingsInserted`, `postingsUpdated`, `postingsSeen`, `postingsExpired`. `postingsSaved`, `newPostings`, `updatedPostings` van duoc giu de backward-compatible.

Internal endpoints moi:

```http
POST /api/internal/market-pulse/refresh
POST /api/internal/market-pulse/ingest
```

Ca hai endpoint can header:

```text
X-Market-Pulse-Key: <MarketPulse:InternalApiKey>
```

`MarketPulse:InternalApiKey` phai duoc cau hinh bang env/user-secrets/secret
va co do dai toi thieu 16 ky tu. Khong commit key that vao source.

## Frontend Market Pulse UX da ap dung

Trang `/market-pulse` hien la lop phan tich tu DB-first overview, khong goi
truc tiep Jobs API/ngrok trong request cua browser. UI su dung cung query
options public overview da ho tro:

- Period filter: 7/14/30/90 ngay, backend van clamp 7..180 ngay.
- Filter bar: role/category, location, experience, source, salary range va skill focus toi da 6 skill.
- Insight cards theo ngu canh: top rising skill, most demanded role, salary-backed signal, data confidence va what changed.
- Data quality notice: backend unavailable/permission issue, no snapshot, no matching market signal, stale/warning tu `dataQuality`.
- Chart demand movement co tooltip period/source/sample size va phan biet series bang mau + dashed line + marker number.
- Skill click mo `learning-modules/browse?q=<skill>`, role/category click mo `roadmaps?q=<role>`.
- Job list co requirement breakdown drawer trong UI, van giu link source goc TopCV/Jobs API.

Trang `/roadmaps` da doc query `?q=`/`?role=` de Market Pulse co the mo
roadmap suggestion da loc theo role/category. Khong can migration/backend
contract moi cho phase UX nay.

## CI/CD, security, observability da ap dung

Roadmap Platform co workflow PR/push `.github/workflows/ci.yml`:

- .NET: restore, build solution, va tu dong chay `dotnet test` neu sau nay co test project.
- Frontend: `npm ci`, `npm run lint`, `npm run build`.
- Guardrails: chan `.env`, SQLite DB/runtime files, hardcoded ngrok host,
  `UserSecretsId`, `Password=123456`, `change-me` trong project/config files.

Security config:

- `appsettings.json` khong con chua database password mau. Dat
  `ConnectionStrings__DefaultConnection` bang env/deployment secret/local secret.
- Neu connection string thieu, backend fail fast voi thong bao ro rang.
- Cac project `.csproj` khong commit `UserSecretsId`. Neu can user-secrets local,
  chay `dotnet user-secrets init` tren may local va khong commit thay doi do.

Observability:

- `GET /health` public: process song.
- `GET /ready` public: kiem tra database reachable, tra `503` khi DB loi.
- `GET /api/Home/check-connection` van duoc bao ve bang
  `system_health.view.any` cho diagnostics co RBAC.

## Du lieu dau vao

Jobs API v1 response du kien:

```json
{
  "ok": true,
  "data": [
    {
      "id": "topcv:2196583",
      "source": "topcv",
      "source_job_id": "2196583",
      "title": "Java Backend Developer",
      "company": "Example Company",
      "category": "Backend",
      "category_normalized": "Backend",
      "post_date": "2026-06-11",
      "post_date_text": "Today",
      "is_active": true,
      "requirements": [],
      "specialties": [],
      "benefits": [],
      "skills_normalized": [],
      "salary": "20 - 35 trieu",
      "experience": "2 nam",
      "location": "Ha Noi",
      "primary_city": "Ha Noi",
      "url": "https://www.topcv.vn/...",
      "updated_at": "2026-06-11T01:04:22.795698"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 100,
    "total": 1,
    "totalPages": 1
  },
  "meta": {
    "source": "topcv",
    "generatedAt": "2026-06-18T09:00:00Z",
    "latestSuccessfulCrawlAt": "2026-06-18T08:05:00Z"
  }
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
- `skills`

Luu y khach quan: API hien tai khong tra full job description, nen chart la market pulse dua tren summary fields, khong phai full-text market intelligence.

## Database va scaffold

Job Market tiep tuc dung cac table san co de khong pha database cua team:

- `job_portal_source`
- `job_posting`
- `job_posting_daily_snapshot`
- `skill_trend_snapshot`

Migration typed field:

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

Phase 2 analytical schema da duoc ap dung trong code hien tai. Cac table moi:

- `job_posting_version`
- `job_posting_observation`
- `skill_taxonomy`
- `job_skill_mention`
- `job_market_daily_snapshot`
- `market_pulse_insight_snapshot`

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

Endpoint Roadmap Platform:

```http
GET /api/market-pulse/overview?days=14&skills=react&skills=node-js
```

Query params:

| Param | Kieu | Mac dinh | Rule |
|---|---:|---:|---|
| `days` | number | `30` | Backend clamp trong khoang `7..180`; UI hien co cac nut `7`, `14`, `30`, `90`. |
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
| `MarketPulse:ActiveJobsApiUrl` | Endpoint active jobs. Nen dung `/api/v1/jobs?active=true&sort=post_date_desc`. |
| `MarketPulse:TodayJobsApiUrl` | Endpoint jobs posted today. Nen dung `/api/v1/jobs/today`. |
| `MarketPulse:JobsApiPageSize` | So job moi page khi goi Jobs API co pagination. |
| `MarketPulse:JobsApiMaxPages` | Gioi han so page toi da moi lan goi live API. |
| `MarketPulse:OverviewCacheSeconds` | Cache public overview 0..300 giay. Dat `0` de tat cache khi debug. |
| `MarketPulse:InternalApiKey` | API key rieng cho `/api/internal/market-pulse/*`. Dat bang secret/env, khong commit gia tri that. |
| `MarketPulse:TrackedKeywords` | Danh sach keyword specs. Dung `|` de khai bao alias, vi du `React|React.js|ReactJS`. |
| `MarketPulse:Sources[].Kind` | `JobsApi` cho source Jobs API, `Html` cho fallback generic HTML scraper. |
| `MarketPulse:Sources[].SearchUrlTemplate` | URL de scheduled refresh doc data source. Nen dung `/api/v1/jobs?active=true&sort=post_date_desc`. |
| `MarketPulse:MaxPostingsPerSource` | Gioi han so postings persist khi chay scheduled refresh. |
| `MarketPulse:RequestTimeoutSeconds` | Timeout HTTP client, clamp `5..120` giay. |
| `MarketPulse:ActivePostingLookbackDays` | Lookback khi tinh active postings trong DB read model. |
| `MarketPulse:MissingScansBeforeStale` | So lan khong thay job truoc khi danh dau stale trong DB lifecycle. |
| `MarketPulse:MinimumPostingsForLifecycleCheck` | Nguong an toan truoc khi mark missing jobs, tranh source fail lam tat ca job bi stale. |

## Cach chay local

Backend:

```powershell
cd src/backend
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=roadmap_platform;Username=postgres;Password=<local-password>"
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
Invoke-RestMethod "http://127.0.0.1:5208/api/market-pulse/overview?days=14&skills=react&location=Ha%20Noi"
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
npx.cmd eslint src/pages/MarketPulsePage.jsx src/pages/RoadmapSelectionPage.jsx src/api/marketPulseApi.js
```

Checklist special cases can kiem tra khi review Job Market:

- Jobs API v1 envelope co `ok`, `data`, `pagination`, `meta`.
- Pagination khong tra full dataset vo han.
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
