# Market Pulse TopCV-only delivery handoff

## Delivered outcome

Market Pulse now treats TopCV as the only product data provider. Python owns the TopCV crawler and its operational evidence, .NET imports the canonical job history and calculates publication-date demand, and React presents the public analytics page plus an admin Operations Console.

The public chart answers **when jobs were posted**, not when they were crawled. Crawler timestamps are used only for relative-date parsing, freshness, completeness, and the analysis anchor. The UI remains English-only and follows TechMap's cream/green design system.

## Public Market Pulse

The page is organized into five focused sections:

1. **Market snapshot**: active jobs, estimated postings in the selected period, change versus the previous period, and top skill.
2. **Trend analysis**: publication demand and up to three skill-demand series, with exact and estimated components, comparison metrics, confidence, tooltips, legend toggles, and an accessible table fallback.
3. **Demand breakdown**: skills, roles, locations, and seniority using real percentages and counts.
4. **Learning signals**: top recommendations and common skill combinations; a recommendation opens the relevant skill trend.
5. **About this data**: TopCV provenance, publication-date methodology, coverage, date quality, salary limitations, and latest successful crawler time.

The public reload button only repeats `GET /api/market-pulse/overview`. It never starts the crawler. Initial loading uses skeletons; a refetch retains prior data; failed refetches do not erase a usable result; unavailable historical dates render as gaps rather than zero.

## Publication analytics model

`publicationAnalytics` uses:

- `basis = published_date`;
- `dateModel = interval_weighted`;
- equal current and previous windows of 7, 14, 30, or 90 days;
- a Vietnam business-date anchor from the latest complete TopCV crawler result imported successfully.

Each exact posting contributes one unit to its publication date. A relative day contributes one estimated unit to its resolved day. A relative week or month contributes a total weight of one distributed uniformly over its retained lower and upper bounds. An interval crossing a period boundary is split, so a posting is never counted twice. Unknown dates remain in quality coverage but not demand totals.

Analytics include active, stale, and expired jobs. Category, location, seniority, and salary filters are applied consistently to current totals, previous totals, market trend, and skill trend. Default skill series are ranked by weighted, reliable demand in the current covered period rather than by raw unknown-date mentions.

A covered day with no posting is zero in the retained TopCV dataset. A day outside retained history coverage is unavailable. A positive current period over a zero previous period is labeled `new` only when the previous period is fully covered.

## Proven history and safe synchronization

Python persists `crawl_runs.is_complete`. Legacy successes are migrated as incomplete and are intentionally not trusted. A listing crawl is complete only when it starts at page 1, reaches the end of the listing, has no blocking/failure condition, and retains its distributed lease. Hitting `max_pages`, resuming from a suffix checkpoint, losing the lease, or receiving blocked/partial output cannot deactivate jobs and cannot publish `latestSuccessfulCrawlAt`.

After a newly proven complete listing run exists, the Jobs API exposes `meta.historyCoverageStart` as the earliest retained canonical job `first_seen_at`, falling back to that complete run for an empty database. .NET clamps this retained-history boundary to the requested lookback. It is not a claim that TopCV itself had zero postings before the crawler began; missing evidence keeps comparisons insufficient.

Historical sync fetches `scope=all`, imports active and inactive rows, skips missing-item lifecycle, and shares the PostgreSQL advisory lock with normal imports. An older payload cannot overwrite a newer successful import. Repeating the same crawler watermark still applies later Python backfill/detail corrections, but does not increment canonical `SeenCount` or rewrite unchanged posting timestamps.

After a full history sync, fresh complete daily imports may extend `coverageEnd` only to the same or adjacent Vietnam business date. A gap cannot be jumped; run history sync again to re-establish a continuous watermark.

## TopCV Admin Operations Console

The first admin load makes one dashboard request. Detailed tabs load only when opened.

The console provides:

- overall pipeline state and latest successful end-to-end refresh;
- a durable `TopCV crawler -> .NET import -> Analytics ready` stepper;
- active jobs, seven-day estimated postings, crawler freshness, reliable date coverage, import lag, and open crawler/import failures;
- separate crawler, import, history, and data-quality health;
- severity-based alerts with suggested actions;
- a mini publication-demand chart and recent operations;
- lazy Import runs, Failures, and Classifier tabs;
- advanced import-latest and historical-sync actions.

`POST /api/market-pulse/admin/refresh-operations` creates a durable operation and returns `202`. A duplicate active request returns `409` with the current operation. The worker waits for a newer complete crawler success before importing, survives API restarts, uses a cross-process advisory lock, and times out instead of occupying the active slot forever.

Import failure retry performs one real fresh complete import for the selected rows. Crawler retry is consumed by the Python retry worker; it is not a status-only queue. Ignore and retry updates are guarded so stale requests cannot reopen resolved or ignored failures.

## Storage refactor

Python retains only:

- `jobs`;
- `crawl_runs`;
- `failed_crawl_items`;
- `pipeline_locks`;
- `crawl_checkpoints`;
- `raw_job_snapshots`;
- `schema_migrations`.

PostgreSQL retains canonical `job_posting`, renamed import run/failure tables, classifier mappings, the publication-history singleton, and durable refresh operations. Provider rows/columns, source-health storage, crawler-observation tables, and duplicate Python analytics tables are removed. `topcv:<source_job_id>` remains the external contract identity, while `source: "topcv"` is derived for compatibility instead of persisted repeatedly.

## Compatibility window

- Legacy `source=topcv` remains accepted; any other provider returns `400`.
- Legacy active URLs/parameters are normalized before a history `scope=all` request.
- Job DTOs still show TopCV provenance.
- `observationAnalytics` is null/retired for one release.
- `/crawl-runs`, `/failed-items`, and `/source-health` remain aliases for one release.

## Safe rollout

Follow [operations-runbook.md](./operations-runbook.md) in order. The mandatory sequence is:

1. stop both Python roles and back up SQLite;
2. dry-run and apply the TopCV-only SQLite migration;
3. dry-run and apply publication-date backfill;
4. back up PostgreSQL and explicitly run consolidated migration 039;
5. rebuild and test both repositories;
6. start Python and complete at least one full page-1-to-end listing crawl;
7. run .NET historical sync;
8. start the API and frontend, then perform the smoke checks.

The consolidated 039 must be run explicitly once even if an older file called 039 was previously recorded. No live database migration is performed by the source package itself.
