# Market Pulse bằng GitHub Actions miễn phí

Tài liệu này hướng dẫn chạy Market Pulse crawler bằng GitHub Actions thay vì Render Cron Job.

## Mục tiêu

Web API trên Render chỉ đọc dữ liệu Market Pulse từ database. Crawler chạy riêng bằng GitHub Actions theo lịch, sau đó ghi dữ liệu vào Supabase/PostgreSQL.

Luồng chạy:

```text
GitHub Actions schedule / manual trigger
  -> dotnet run RoadmapPlatform.MarketPulseJob
  -> Python TopCV scraper
  -> Upsert Supabase/PostgreSQL
  -> Web API đọc dữ liệu đã có sẵn
```

## File chính

```text
.github/workflows/market-pulse-refresh.yml
src/backend/RoadmapPlatform.MarketPulseJob/
src/backend/RoadmapPlatform.Api/Scrapers/topcv_market_pulse.py
```

## Lịch chạy mặc định

Workflow đang dùng:

```yaml
schedule:
  - cron: "30 19 * * *"
```

GitHub Actions mặc định tính cron theo UTC. `19:30 UTC` tương đương `02:30 sáng Việt Nam`.

## Bước 1: tắt crawler tự chạy trong Web API

Trong `src/backend/RoadmapPlatform.Api/appsettings.json`, để:

```json
"MarketPulse": {
  "Enabled": false,
  "RunOnStartup": false,
  "DailyRunTime": "19:30"
}
```

Lý do: Web API không nên crawl lúc user mở web. Web API chỉ nên trả API đọc dữ liệu.

## Bước 2: thêm GitHub Secret

Vào GitHub repo:

```text
Settings -> Secrets and variables -> Actions -> New repository secret
```

Tạo secret:

```text
MARKET_PULSE_DB_CONNECTION_STRING
```

Value là connection string Supabase/PostgreSQL, ví dụ:

```text
Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;GSS Encryption Mode=Disable
```

Không commit connection string thật vào code.

## Bước 3: tùy chọn biến cấu hình

Bạn có thể tạo Repository Variables ở:

```text
Settings -> Secrets and variables -> Actions -> Variables
```

Các biến tùy chọn:

```text
MARKET_PULSE_MAX_PAGES=4
MARKET_PULSE_MAX_POSTINGS=160
MARKET_PULSE_REQUEST_DELAY_SECONDS=2
MARKET_PULSE_REQUEST_TIMEOUT_SECONDS=30
```

Nếu không tạo variables, workflow sẽ dùng default trong file YAML.

## Bước 4: chạy thử thủ công

Sau khi push workflow lên branch default, vào:

```text
GitHub repo -> Actions -> Refresh Market Pulse -> Run workflow
```

Manual run mặc định dùng:

```text
max_pages = 2
max_postings = 80
```

Bạn có thể tăng dần khi thấy chạy ổn.

## Bước 5: kiểm tra kết quả

Trong tab Actions, mở run mới nhất. Nếu thành công, log sẽ có dạng:

```text
Starting Market Pulse cron refresh...
Market Pulse result: snapshotDate=..., scraped=..., saved=..., new=..., updated=...
Market Pulse cron refresh finished successfully...
```

Sau đó mở web Market Pulse hoặc gọi API:

```http
GET /api/market-pulse/overview
```

## Lưu ý quan trọng

1. Workflow `schedule` chỉ chạy khi file workflow nằm trên default branch, thường là `main`.
2. GitHub có thể delay scheduled workflow vào lúc tải cao. Không nên đặt đúng phút `00`; file này dùng phút `30`.
3. Public repo dùng standard GitHub-hosted runners miễn phí. Private repo có quota free minutes theo plan.
4. Nếu public repo không có activity trong 60 ngày, scheduled workflows có thể bị GitHub tự động disable.
5. Không dùng workflow này để crawl quá dày, vì có thể tốn GitHub Actions minutes, Supabase quota và làm nguồn dữ liệu chặn crawler.

## Khi nào nên dùng GitHub Actions thay Render Cron?

Nên dùng khi:

- Muốn tiết kiệm chi phí.
- Job chỉ chạy 1-2 lần/ngày.
- Không cần worker luôn bật.
- Crawler có thể hoàn thành trong vài chục phút.

Không nên dùng nếu:

- Cần crawl liên tục theo phút.
- Job rất lâu hoặc cần queue phức tạp.
- Cần retry/phân phối worker chuyên nghiệp.

## Debug nhanh

Nếu workflow fail vì thiếu secret:

```text
Missing MARKET_PULSE_DB_CONNECTION_STRING secret.
```

Thêm secret trong GitHub Actions settings.

Nếu fail vì database:

```text
NpgsqlException / PostgresException / relation does not exist
```

Kiểm tra connection string và đã chạy migration `005-market-pulse.sql` chưa.

Nếu scraper trả 0 jobs:

- TopCV có thể chặn request tạm thời.
- Giảm `MARKET_PULSE_MAX_POSTINGS`.
- Tăng `MARKET_PULSE_REQUEST_DELAY_SECONDS`.
- Kiểm tra log Python diagnostics trong workflow.
