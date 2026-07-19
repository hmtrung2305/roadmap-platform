# Market Pulse TopCV-only architecture

## Product boundary

TopCV is the only provider supported by Market Pulse. The provider is a product invariant, not a configurable source. Adding another provider later requires a new feature, contract review, and database migration.

```text
TopCV
  -> Python crawler and scheduler
  -> TopCV-only SQLite store
  -> Python Jobs API
  -> .NET TopCvJobsApiClient
  -> PostgreSQL canonical jobs and import operations
  -> publication-date analytics
  -> public Market Pulse and Admin Operations Console
```

Python owns crawling, parsing TopCV relative dates, raw snapshots, crawler runs, checkpoints, locks, and crawler failures. .NET owns canonical imports, lifecycle, historical synchronization, publication analytics, persistent end-to-end refresh operations, import failures, and the React-facing API. React talks only to .NET.

`topcv:<source_job_id>` remains the external ID format. API responses may still include `source: "topcv"` for provenance and compatibility, but neither database stores a repeated provider column.

## Python runtime store

The retained SQLite tables are:

- `jobs`;
- `crawl_runs`;
- `failed_crawl_items`;
- `pipeline_locks`;
- `crawl_checkpoints`;
- `raw_job_snapshots`;
- `schema_migrations`.

The TopCV-only migration rejects any non-TopCV legacy row before it changes the schema. It rebuilds source-bearing tables transactionally, verifies retained row counts, runs `PRAGMA foreign_key_check`, and requires `PRAGMA integrity_check = ok` before commit.

The former derived analytics tables are dropped because PostgreSQL/.NET owns Market Pulse analytics. Raw snapshots remain for replay and debugging. Their default retention is 30 days. Closed crawl runs and resolved/ignored failures are retained for 180 days; open failures are never removed by retention.

The API accepts legacy `source=topcv` and returns HTTP 400 for every other source. Job list scope is `active`, `inactive`, or `all`; the default is `active`, while the old `active=true|false` query remains compatible.

## PostgreSQL runtime store

The consolidated migration is `database/migrations/039-market-pulse-topcv-consolidated.sql`. It is transactional and idempotent, and it must be run once even when an older migration named 039 was previously applied.

Market Pulse retains:

- `job_posting`, with globally unique `external_id`;
- `market_pulse_import_run`;
- `market_pulse_import_failure`;
- `market_pulse_classifier_keyword_mapping`;
- singleton `market_pulse_publication_history_state`;
- `market_pulse_refresh_operation`.

The migration removes the provider table/FK, source columns/indexes, source-health table, and crawler-observation analytics tables. It aborts if legacy PostgreSQL rows identify a provider other than TopCV. Job IDs, external IDs, import run/failure IDs, and classifier mappings are preserved.

## Publication-date model

Public trend analytics answer "when were jobs posted?" Crawler timestamps never become the chart axis. They are used only to:

- anchor relative TopCV labels;
- determine freshness and completeness;
- select the latest successful crawler business date in `Asia/Ho_Chi_Minh`.

For each posting:

- exact date: weight `1` on one date;
- relative day: weight `1` on its resolved date and marked estimated;
- relative week/month: total weight `1` distributed uniformly over `[post_date_lower_bound, post_date_upper_bound]`;
- unknown: excluded from demand totals but included in date-quality coverage.

Interval weights may cross a period boundary. Only the intersecting fraction contributes to each period, so one posting is never double-counted. Current and previous periods are equal-length 7, 14, 30, or 90-day windows anchored to the latest successful TopCV crawl date.

Analytics include active, stale, and expired canonical jobs. Category, location, seniority, and salary filters apply before all current/previous totals, market points, and skill points. A covered date with no matching posting is zero in the retained TopCV dataset; a date outside the publication-history watermark is unavailable.

Comparison state is `insufficient` until the required historical window is covered. A positive current period over a zero previous period becomes `new` only when the previous period is fully covered.

## Historical synchronization

Python's `crawler.post_date_backfill` repairs only unknown rows that retain `post_date_text`. It anchors the production parser to `last_seen_at`, falling back to `first_seen_at`, interpreting stored timestamps as UTC and resolving them in Vietnam business time. It never overwrites exact or relative evidence and is idempotent.

.NET historical sync fetches `scope=all` for up to 400 days. Inactive Python rows become expired canonical rows. Historical sync does not run missing-item lifecycle and does not mutate normal import-health semantics. It shares the PostgreSQL advisory lock used by regular imports. The history watermark advances only after every page commits successfully.

Python marks a listing run complete only after a page-1-to-end scan. Page caps, resumed suffixes, block/layout failures, and lost leases remain partial, cannot deactivate jobs, and cannot publish freshness. Once a newly proven complete run exists, the Jobs API exposes the earliest retained canonical job `first_seen_at` as `meta.historyCoverageStart` (or the complete run when the database is empty). This bootstraps usable pre-migration history while explicitly describing coverage as local retained-data coverage, not proof that TopCV had no other postings on every intervening day.

Repeating historical sync at the same crawler timestamp is allowed because Python backfill and detail enrichment can change the dataset without changing the listing watermark. The repeat upserts corrections while preserving canonical seen counters and unchanged timestamps. Older crawler payloads are rejected after the PostgreSQL advisory lock is acquired.

No posting publication date is synthesized from `first_seen_at`, `last_seen_at`, or an arbitrary midpoint. Those crawler timestamps only establish retained-data coverage and anchor the production parser when `post_date_text` itself supplies relative-date evidence.

## End-to-end refresh operation

The Operations Console starts a durable `market_pulse_refresh_operation`:

```text
queued -> crawling -> importing -> success
                    \-> failed
```

Only one operation may be active. A duplicate request returns HTTP 409 with the current operation. The coordinator captures a crawler baseline, triggers a TopCV listing crawl, and imports only after a newer successful, complete crawl appears. Blocked, partial, failed, stale, or timed-out crawler work never enters import.

The database state lets the UI resume polling after a browser reload or API restart. The public "Reload market insights" action remains a read-only GET and never starts a crawler.

## API compatibility window

- Public `source` query: null or `topcv` is accepted; other values receive a clear validation error. The frontend no longer sends or displays this filter.
- Job DTO provenance remains `TopCV`.
- `observationAnalytics` is returned as null/retired for one release. No observation tables or calculation code remain.
- Legacy admin routes `/crawl-runs`, `/failed-items`, and `/source-health` remain aliases for one release; new UI uses `/dashboard`, `/import-runs`, `/failures`, and `/refresh-operations`.

## Runtime roles

- `jobs-api-web`: serves readiness, TopCV jobs, and protected crawler operations.
- `jobs-scheduler`: owns scheduled listing/detail crawls against the shared SQLite volume.
- `RoadmapPlatform.MarketPulseJob`: imports the latest completed crawl or runs `--mode history-sync`.
- `RoadmapPlatform.Api`: public overview, admin dashboard, operation coordinator, and polling APIs.
- React: publication-demand UX and TopCV Operations Console.

Only one Python scheduler should target a database. Python and .NET use independent distributed locks for their respective stores.
