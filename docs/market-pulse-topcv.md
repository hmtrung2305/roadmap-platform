# Market Pulse - TopCV Data Pipeline

## Nguồn TopCV Đang Dùng

Market Pulse không dùng URL search keyword rộng như:

```text
https://www.topcv.vn/tim-viec-lam-<keyword>?page=<page>
```

Thay vào đó, crawler dùng feed category Công nghệ thông tin của TopCV và sắp xếp theo việc mới:

```text
https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257?sort=new&type_keyword=0&page={page}&category_family=r257&saturday_status=0
```

Lý do:

- `cong-nghe-thong-tin-cr257` giới hạn nguồn vào nhóm job CNTT trên TopCV.
- `sort=new` ưu tiên các job mới đăng/cập nhật gần nhất, phù hợp với snapshot hằng ngày.
- `page={page}` giúp scheduler lấy page 1, 2, ... theo `MaxPagesPerSource`.
- Script cũng hỗ trợ placeholder `page={1}` nếu cần paste đúng format URL TopCV.
- Category feed có thể có một số job ít kỹ thuật; snapshot chỉ tính các skill có match trong
  `TrackedKeywords`, nên job không có signal công nghệ sẽ không tạo skill trend giả.

Tài liệu này mô tả chi tiết chức năng **Market Pulse** sau khi chỉnh lại để giữ
TopCV là nguồn dữ liệu chính.

## Mục Tiêu

Market Pulse cần làm 3 việc:

1. Crawl dữ liệu job IT từ TopCV theo lịch hằng ngày.
2. Phân tích tần suất keyword trong mô tả công việc để tìm công nghệ đang được
   tuyển nhiều.
3. Hiển thị biểu đồ xu hướng trên frontend.

## Kiến Trúc Tổng Quan

```text
TopCV public pages
        |
        v
Python crawler: RoadmapPlatform.Api/Scrapers/topcv_market_pulse.py
        |
        | JSON normalized postings
        v
.NET adapter: JobPortalScraper
        |
        v
MarketPulseService
        |
        +--> job_portal_source
        +--> job_posting
        +--> job_posting_daily_snapshot
        +--> skill_trend_snapshot
        |
        v
API: GET /api/market-pulse/overview
        |
        v
Frontend: /market-pulse
```

Lý do dùng Python cho tầng crawl:

- TopCV là HTML site, không phải API ổn định.
- Python script tách riêng phần ingestion/parsing, dễ smoke test độc lập.
- Backend .NET vẫn giữ vai trò scheduling, persistence, keyword analysis và API.

## File Quan Trọng

Backend:

```text
src/backend/RoadmapPlatform.Api/Scrapers/topcv_market_pulse.py
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/JobPortalScraper.cs
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/MarketPulseService.cs
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/MarketPulseHostedService.cs
src/backend/RoadmapPlatform.Infrastructure/Services/MarketPulse/MarketPulseKeywordAnalyzer.cs
src/backend/RoadmapPlatform.Infrastructure/Data/ApplicationDbContext.MarketPulse.cs
src/backend/RoadmapPlatform.Infrastructure/Entities/JobPortalSource.cs
src/backend/RoadmapPlatform.Infrastructure/Entities/JobPosting.cs
src/backend/RoadmapPlatform.Infrastructure/Entities/SkillTrendSnapshot.cs
src/backend/RoadmapPlatform.Api/Controllers/MarketPulse/MarketPulseController.cs
```

Database:

```text
docs/database/migrations/004-market-pulse.sql
```

Frontend:

```text
src/frontend/src/api/marketPulseApi.js
src/frontend/src/pages/MarketPulsePage.jsx
src/frontend/src/App.jsx
src/frontend/src/components/layout/TopNavBar.jsx
```

## Database

Chạy migration:

```text
docs/database/migrations/004-market-pulse.sql
docs/database/migrations/005-market-pulse-job-lifecycle.sql
```

Nếu database chưa từng chạy Market Pulse thì `004-market-pulse.sql` đã chứa schema mới đầy đủ.
Nếu database đã chạy bản Market Pulse cũ, chạy thêm `005-market-pulse-job-lifecycle.sql` để nâng cấp
mà không mất dữ liệu đã crawl.

Migration tạo/cập nhật các bảng chính:

```text
job_portal_source
job_posting
job_posting_daily_snapshot
skill_trend_snapshot
```

Kiểm tra bảng đã có:

```sql
select table_name
from information_schema.tables
where table_schema = 'public'
  and table_name in ('job_portal_source', 'job_posting', 'job_posting_daily_snapshot', 'skill_trend_snapshot');
```

Kiểm tra dữ liệu crawl:

```sql
select
  source.name,
  count(*) as posting_count,
  count(*) filter (where posting.lifecycle_status = 'active') as active_count,
  count(*) filter (where posting.lifecycle_status = 'stale_unverified') as stale_unverified_count,
  count(*) filter (where posting.lifecycle_status = 'expired') as expired_count,
  max(posting.last_seen_at) as last_seen_at
from public.job_posting posting
join public.job_portal_source source
  on source.job_portal_source_id = posting.job_portal_source_id
group by source.name;
```

Kiểm tra observation theo ngày:

```sql
select snapshot_date, observation_status, count(*) as job_count
from public.job_posting_daily_snapshot
group by snapshot_date, observation_status
order by snapshot_date desc, observation_status;
```

Kiểm tra snapshot skill:

```sql
select snapshot_date, skill_name, mention_count, posting_count
from public.skill_trend_snapshot
order by snapshot_date desc, mention_count desc
limit 20;
```

## Cấu Hình Backend

File:

```text
src/backend/RoadmapPlatform.Api/appsettings.json
```

Phần chính:

```json
"MarketPulse": {
  "Enabled": true,
  "RunOnStartup": false,
  "DailyRunTime": "02:30",
  "SearchKeyword": "cong nghe thong tin",
  "MaxPagesPerSource": 4,
  "MaxPostingsPerSource": 160,
  "ActivePostingLookbackDays": 14,
  "MissingScansBeforeStale": 3,
  "MinimumPostingsForLifecycleCheck": 30,
  "RequestDelaySeconds": 2,
  "RequestTimeoutSeconds": 30,
  "PythonExecutablePath": "python",
  "PythonScriptPath": "Scrapers/topcv_market_pulse.py",
  "Sources": [
    {
      "Name": "TopCV",
      "Kind": "TopCvPython",
      "BaseUrl": "https://www.topcv.vn",
      "SearchUrlTemplate": "https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257?sort=new&type_keyword=0&page={page}&category_family=r257&saturday_status=0",
      "Enabled": true,
      "DetailUrlContains": ["/viec-lam/"]
    }
  ]
}
```

Ý nghĩa:

- `Enabled`: bật/tắt scheduler Market Pulse.
- `RunOnStartup`: nếu `true`, crawl ngay khi backend start. Dùng để test local.
- `DailyRunTime`: giờ chạy hằng ngày theo giờ máy/server.
- `SearchKeyword`: keyword dự phòng; với URL category hiện tại thì template không cần thay keyword.
- `MaxPagesPerSource`: số trang search TopCV cần đọc.
- `MaxPostingsPerSource`: số job detail tối đa cần parse mỗi lần chạy.
- `ActivePostingLookbackDays`: số ngày gần nhất dùng để xem job là active signal cho trend.
- `MissingScansBeforeStale`: số lần refresh khác ngày không thấy lại job trước khi chuyển sang
  `stale_unverified`.
- `MinimumPostingsForLifecycleCheck`: số job tối thiểu cần parse trong một lần crawl trước khi hệ
  thống được phép tăng `missing_scan_count` cho job không thấy lại. Guard này tránh đánh dấu stale
  sai khi TopCV/network chỉ trả về batch nhỏ bất thường.
- `RequestDelaySeconds`: nghỉ giữa các request detail. Nên giữ lớn hơn 0.
- `RequestTimeoutSeconds`: timeout mỗi HTTP request trong Python crawler.
- `PythonExecutablePath`: đường dẫn Python. Có thể để `python` nếu Python đã nằm
  trong PATH.
- `PythonScriptPath`: đường dẫn script crawler. File `.csproj` đã cấu hình copy
  script này ra output khi build/publish.

Nếu máy không nhận `python`, có 2 lựa chọn:

```powershell
dotnet user-secrets set "MarketPulse:PythonExecutablePath" "C:\Path\To\python.exe" --project src\backend\RoadmapPlatform.Api\RoadmapPlatform.Api.csproj
```

Hoặc sửa trực tiếp `appsettings.Development.json`:

```json
"MarketPulse": {
  "PythonExecutablePath": "C:\\Path\\To\\python.exe"
}
```

Backend cũng có fallback thử các lệnh:

```text
python
py -3
python3
```

nhưng cấu hình absolute path vẫn là cách ổn định nhất.

## Chạy Backend

```powershell
cd src\backend
dotnet build RoadmapPlatform.slnx
dotnet run --project RoadmapPlatform.Api --launch-profile https
```

Backend chạy ở:

```text
https://localhost:7103
http://localhost:5237
```

Nếu chỉ muốn kiểm tra crawl ngay, set:

```json
"RunOnStartup": true
```

Sau đó restart backend.

## Chạy Frontend

```powershell
cd src\frontend
npm install
npm run dev
```

Mở:

```text
http://localhost:5173/market-pulse
```

## Smoke Test Python Crawler

Chạy độc lập để xác nhận TopCV đang crawl được trước khi debug backend:

```powershell
python src/backend/RoadmapPlatform.Api/Scrapers/topcv_market_pulse.py `
  --source-name TopCV `
  --base-url https://www.topcv.vn `
  --search-url-template "https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257?sort=new&type_keyword=0&page={page}&category_family=r257&saturday_status=0" `
  --keyword "cong nghe thong tin" `
  --pages 1 `
  --limit 3 `
  --delay 1 `
  --timeout 20
```

Kết quả đúng:

- stdout: JSON array các job đã normalize.
- stderr/log: các dòng diagnostic.

Ví dụ diagnostic đúng:

```text
TopCV search page 1 yielded 46 candidate job links
TopCV detail 1/3 parsed: Nhân Viên Quản Trị Dữ Liệu (Data Admin )
TopCV scrape finished with 3 normalized postings
```

Nếu command trả exit code `0`, crawler OK.

## Log Backend Đúng

Khi backend chạy đúng, log sẽ có dạng:

```text
TopCV Python scraper diagnostics: TopCV scrape started ...
TopCV search page 1 yielded 46 candidate job links
TopCV detail 1/160 parsed: ...
TopCV scrape finished with ... normalized postings
TopCV Python scraper returned ... normalized postings using python.
Market Pulse refreshed ... postings from 1 sources; new=..., updated=..., active=..., stale=..., expired=..., skillSnapshots=....
```

Nếu thấy:

```text
TopCV Python scraper exited with code 1
Market Pulse refreshed 0 postings
```

nghĩa là Python script crash. Chạy smoke test Python để xem traceback đầy đủ.

## API

Endpoint frontend dùng:

```http
GET /api/market-pulse/overview?days=30
```

Có thể lọc skill:

```http
GET /api/market-pulse/overview?days=30&skills=react&skills=python
```

Response gồm:

- `lastUpdatedAt`
- `totalPostings`
- `sourceCount`
- `skills`
- `trendPoints`

## Keyword Analysis

Keyword được cấu hình trong `MarketPulse:TrackedKeywords`.

Ví dụ:

```json
"React|React.js|ReactJS"
```

Ý nghĩa:

- `React` là tên skill hiển thị.
- Các alias sau dấu `|` đều được tính là mention của skill đó.

Analyzer đếm:

- `mentionCount`: tổng số lần keyword xuất hiện.
- `postingCount`: số job có nhắc tới keyword.

## Data Semantics Theo Ngày

Market Pulse không chỉ đếm batch vừa crawl. Pipeline hiện lưu state theo từng job và observation
theo từng ngày.

Trạng thái job:

- `active`: job được thấy lại trong crawler gần đây và chưa quá hạn nộp hồ sơ.
- `stale_unverified`: job không xuất hiện trong cửa sổ crawl mới nhất đủ nhiều lần theo
  `MissingScansBeforeStale`, và chỉ được tăng missing count khi batch crawl đạt
  `MinimumPostingsForLifecycleCheck`. Trạng thái này không khẳng định job đã đóng tuyển; nó chỉ loại
  job khỏi active demand signal để tránh trend bị kéo bởi dữ liệu cũ.
- `expired`: crawler parse được hạn nộp hồ sơ (`ExpiresAt`) và ngày đó đã qua. Đây là tín hiệu mạnh
  hơn để xem job không còn active.

Observation theo ngày trong `job_posting_daily_snapshot`:

- `new`: lần đầu hệ thống thấy job này.
- `seen`: job đã tồn tại và nội dung không đổi trong lần crawl hôm đó.
- `updated`: job đã tồn tại nhưng hash nội dung thay đổi, ví dụ title/mô tả/hạn nộp được cập nhật.

Skill trend được tính trên active corpus trong DB:

```text
job.is_active = true
job.last_seen_at >= now - ActivePostingLookbackDays
job.expires_at is null hoặc chưa quá hạn
```

Nhờ vậy biểu đồ không phụ thuộc hoàn toàn vào 160 job vừa crawl hôm nay; dữ liệu sẽ tốt dần khi hệ
thống tích lũy nhiều ngày observation.

## Cách Dữ Liệu Được Lưu

Mỗi lần refresh:

1. Upsert nguồn TopCV vào `job_portal_source`.
2. Deduplicate URL job theo `(sourceName + url)`.
3. Tính `content_hash` từ title/company/location/description/expiresAt.
4. Upsert `job_posting`, cập nhật first/last seen, missing count, active/stale/expired status.
5. Upsert observation trong `job_posting_daily_snapshot` với `new`, `seen`, hoặc `updated`.
6. Phân tích keyword trên active corpus trong DB, không chỉ batch vừa crawl.
7. Upsert snapshot theo ngày vào `skill_trend_snapshot`.
8. Nếu chạy lại trong cùng ngày và skill cũ không còn trong active corpus mới, snapshot stale của
   ngày đó sẽ bị xóa để dữ liệu phản ánh trạng thái mới nhất.

Các guard chất lượng dữ liệu:

- Nếu crawler trả `0` job, refresh không tạo `skill_trend_snapshot` mới cho ngày đó.
- Nếu batch parse được ít hơn `MinimumPostingsForLifecycleCheck`, hệ thống vẫn lưu các job thấy được
  nhưng không tăng missing count cho các job không thấy lại.
- Phần ghi source/job/observation/skill snapshot chạy trong cùng DB transaction sau khi crawl xong.

## Troubleshooting

### Web chưa có dữ liệu

Kiểm tra DB:

```sql
select count(*) from public.job_posting;
select count(*) from public.skill_trend_snapshot;
```

Nếu cả hai bằng 0, kiểm tra log backend và chạy smoke test Python.

### Crawler có log parsed job nhưng Market Pulse vẫn 0

Tìm log:

```text
TopCV Python scraper exited with code ...
```

Nếu exit code khác 0, backend sẽ bỏ batch để tránh lưu dữ liệu lỗi.

### Lỗi Python không tìm thấy

Set absolute path:

```powershell
dotnet user-secrets set "MarketPulse:PythonExecutablePath" "C:\Users\Admin\AppData\Local\Programs\Python\Python312\python.exe" --project src\backend\RoadmapPlatform.Api\RoadmapPlatform.Api.csproj
```

### Lỗi encoding tiếng Việt trên Windows

Script đã xử lý bằng:

```text
PYTHONIOENCODING=utf-8
PYTHONUTF8=1
```

và JSON output dùng unicode escape an toàn cho stdout.

### TopCV trả 403

Crawler hiện dùng search URL:

```text
https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257?sort=new&type_keyword=0&page={page}&category_family=r257&saturday_status=0
```

với browser-like headers. Nếu TopCV thay đổi anti-bot, smoke test Python sẽ cho
biết status. Không nên cố bypass CAPTCHA/login/private content; nếu bị chặn lâu
dài thì cần nguồn chính thức hoặc thỏa thuận dữ liệu.

### `Cannot create a DbSet for SkillTrendSnapshot`

Thiếu file mapping EF:

```text
src/backend/RoadmapPlatform.Infrastructure/Data/ApplicationDbContext.MarketPulse.cs
```

Đảm bảo file này được copy vào project và build lại backend.

## Checklist Trước Khi Demo

1. Database đã chạy `004-market-pulse.sql`; nếu đã từng chạy bản cũ thì chạy thêm
   `005-market-pulse-job-lifecycle.sql`.
2. Python chạy được:

   ```powershell
   python --version
   ```

3. Smoke test crawler trả JSON và exit code `0`.
4. `RunOnStartup` bật `true` nếu muốn có dữ liệu ngay.
5. Backend log có `Market Pulse refreshed ... postings`.
6. DB có record trong `job_posting`, `job_posting_daily_snapshot` và `skill_trend_snapshot`.
7. Frontend `/market-pulse` hiển thị chart và top skills.

## Ghi Chú Vận Hành

- Không set `RequestDelaySeconds` về 0 khi chạy thật.
- Không tăng `MaxPostingsPerSource` quá cao trong local/demo; tăng dần theo sức chịu tải server và response của TopCV.
- Crawler chỉ đọc public page, không đăng nhập, không giải CAPTCHA.
- Khi TopCV đổi HTML, chỉ cần cập nhật parser Python; database/API/frontend không
  cần đổi nếu output JSON giữ cùng contract.
