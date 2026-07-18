# Market Pulse Architecture

## Ownership boundaries

```text
TopCV
  -> Python crawler/scheduler
  -> Python SQLite/PostgreSQL-compatible job store
  -> Python Jobs API + ops health
  -> .NET MarketPulseJob / admin refresh
  -> PostgreSQL job_posting + import operations tables
  -> .NET Market Pulse API
  -> React frontend
```

Python is the canonical owner of crawling, anti-block handling, listing/detail scheduling, source parsing, normalization, source lifecycle, and crawler health. .NET owns importing the stable Python contract, import lifecycle safety, snapshots/analytics, admin operations, authentication, and the API used by React.

React calls only .NET. The Jobs API key and database credentials therefore remain server-side.

## Retained and removed .NET tables

The retained runtime tables are:

- `job_portal_source` and canonical imported `job_posting`;
- legacy-named `market_pulse_crawl_run`, which now records .NET import runs;
- `market_pulse_failed_item`, limited to .NET import failures;
- `market_pulse_source_health` for persisted importer state;
- `market_pulse_classifier_keyword_mapping` for admin classification rules.

Migration `037` removes the unused/write-only tables `job_posting_daily_snapshot`, `skill_trend_snapshot`, `job_posting_version`, `job_posting_observation`, `skill_taxonomy`, `job_skill_mention`, `job_market_daily_snapshot`, and `market_pulse_insight_snapshot`. Overview analytics are calculated from `job_posting`; normalized skills are stored on that row. Historical migrations remain in the repository for upgrade ordering, but removed tables are not part of the runtime design.

## Runtime processes

- `jobs-api-web`: serves `/health`, `/api/v1/jobs`, and protected `/api/v1/ops/*`; it does not schedule crawling by default.
- `jobs-scheduler`: runs listing and detail crawl schedules against the shared Python data volume.
- `RoadmapPlatform.MarketPulseJob`: validates source freshness, imports paginated active jobs, and returns a non-zero exit code on failure.
- `RoadmapPlatform.Api`: serves public Market Pulse analytics and authenticated admin endpoints.
- React admin: presents Python crawler health separately from .NET import health.

Only one scheduler should target a Python database. GitHub Actions may schedule the .NET import independently, but it must run after a fresh Python crawl.

## Data and failure boundaries

- A stale, missing, critical, unauthorized, or malformed Python health response blocks scheduled .NET ingestion.
- Partial imports never run missing-job lifecycle deactivation.
- A complete, fresh import may increment missing-scan counters and apply lifecycle rules.
- Python crawl failures and .NET import failures are different failure domains. Admin retry commands apply only to .NET failures.
- If one admin panel request fails, other panels continue to render.

## Relative date evidence

Source strings such as `2 weeks ago` do not identify one exact calendar date. Python records an evidence interval:

- `N weeks ago`: from observation date minus `(N + 1)` weeks plus one day, through observation date minus `N` weeks;
- `N months ago`: the equivalent calendar-month interval, with month lengths handled correctly;
- exact/day/hour/minute expressions: a point interval.

The displayed `post_date` is the midpoint estimate for broad intervals. On later crawls, Python intersects the old and new intervals. This narrows the estimate without pretending the first observation was exact. An exact date seen later replaces the relative estimate; an unknown value never destroys previously reliable evidence.

The contract preserves `post_date_lower_bound`, `post_date_upper_bound`, and `post_date_observed_on` through .NET and PostgreSQL. Analytics can continue to use the representative date while future logic can use bounds explicitly.

## Production scheduling

The Python scheduler controls crawl cadence. The `.github/workflows/market-pulse-refresh.yml` schedule controls import cadence. Before importing, the workflow checks Python readiness and freshness. This decoupling prevents an API restart from unexpectedly crawling and prevents an import from treating old source data as current.
