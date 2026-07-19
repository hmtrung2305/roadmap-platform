# Market Pulse TopCV pipeline

Market Pulse is a TopCV-only feature. For the complete design and rollout procedure, see:

- [Architecture](market-pulse/market-pulse-architecture.md)
- [Operations runbook](market-pulse/operations-runbook.md)
- [Python-to-.NET contract](market-pulse/data-contract-python-to-dotnet.md)

## Stable Jobs API contract

Canonical job pages are read from:

```text
GET /api/v1/jobs?page=1&pageSize=100&scope=active
GET /api/v1/jobs?page=1&pageSize=100&scope=all
```

`scope` is `active`, `inactive`, or `all`. Legacy `active=true|false` remains supported and takes precedence. Legacy `source=topcv` is accepted; every other value returns HTTP 400.

Responses continue to derive provenance without storing it:

```json
{
  "ok": true,
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 100,
    "total": 0,
    "totalPages": 0
  },
  "meta": {
    "source": "topcv",
    "generatedAt": "2026-07-18T08:00:00Z",
    "latestSuccessfulCrawlAt": "2026-07-18T07:55:00Z",
    "historyCoverageStart": "2026-06-01T01:00:00Z"
  }
}
```

External IDs stay `topcv:<source_job_id>`.

`latestSuccessfulCrawlAt` only uses proven complete page-1-to-end listing runs. After at least one such run exists, `historyCoverageStart` uses the earliest retained job `first_seen_at` (or the complete run for an empty database), allowing pre-migration canonical history to be synchronized without upgrading a legacy run to lifecycle-safe. The endpoint returns `409` while the listing lease is active so a multi-page import cannot read a mutating crawler dataset.

## .NET configuration

```text
MarketPulse__JobsApiUrl=https://<jobs-api>/api/v1/jobs
MarketPulse__JobsApiKey=<same as Python ADMIN_API_KEY>
MarketPulse__JobsApiOpsHealthUrl=https://<jobs-api>/api/v1/ops/health-summary
MarketPulse__JobsApiCrawlTriggerUrl=https://<jobs-api>/api/crawl/listing/run
MarketPulse__JobsApiCrawlStatusUrl=https://<jobs-api>/api/v1/crawl-runs/latest?pipeline=listing&limit=1
MarketPulse__JobsApiPageSize=100
MarketPulse__JobsApiMaxPages=500
MarketPulse__JobsApiMaxItems=50000
MarketPulse__BusinessTimezone=Asia/Ho_Chi_Minh
```

There are no source arrays, HTML search templates, per-source delays, or source-specific page settings.

## Public analytics

`GET /api/market-pulse/overview?days=30` returns `publicationAnalytics`:

- `basis = published_date`;
- `dateModel = interval_weighted`;
- equal current/previous periods;
- market and skill points with exact, relative-estimated, and total values;
- publication-history watermark and explicit availability;
- post-date quality and confidence.

Exact dates contribute once on their date. Relative week/month ranges distribute one total posting across the evidence interval. Unknown dates affect quality coverage but not demand. Active, stale, and expired jobs are eligible for historical analysis.

The public frontend does not expose a source filter. `source=topcv` remains a backend compatibility shim for one release.

## Admin operations

The Operations Console initially requests only:

```text
GET /api/market-pulse/admin/dashboard
```

Import runs, failures, and classifier data lazy-load when their tab opens. End-to-end refresh uses:

```text
POST /api/market-pulse/admin/refresh-operations
GET  /api/market-pulse/admin/refresh-operations/current
GET  /api/market-pulse/admin/refresh-operations/{id}
```

One operation progresses through `queued`, `crawling`, `importing`, and `success`/`failed`. A concurrent request returns 409 and the active operation. Import begins only after a new successful TopCV listing crawl newer than the captured baseline.

## Initial data preparation

1. Stop Python roles and back up SQLite.
2. Run `python -m scripts.migrate_topcv_only --dry-run`, then `--apply`.
3. Run `python -m crawler.post_date_backfill --dry-run`, then `--apply`.
4. Back up PostgreSQL and run `039-market-pulse-topcv-consolidated.sql`.
5. Start Python roles.
6. Complete and verify one fresh listing run with `status=success` and `is_complete=true`.
7. Run `RoadmapPlatform.MarketPulseJob --mode history-sync --lookback-days 400`.
8. Start the API and frontend.

Do not backfill history from crawler dates alone. The watermark advances only after complete historical pagination commits.
