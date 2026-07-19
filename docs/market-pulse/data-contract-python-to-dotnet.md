# Market Pulse Data Contract: Python to .NET

## Jobs endpoint

The .NET importer reads:

```text
GET /api/v1/jobs?scope=active&page=<n>&pageSize=<n>
```

The response envelope contains `ok`, `data`, `pagination`, and `meta`. `pagination.total`, `page`, `pageSize`, and `totalPages` determine whether the import is complete. Pages use stable canonical job-ID ordering. .NET validates the requested page coordinates, rejects duplicate stable IDs or missing URLs, and calculates completeness from distinct converted rows.

`meta.latestSuccessfulCrawlAt` is required by freshness policy and excludes legacy, capped, resumed, blocked, and partial runs. Once at least one newly proven complete run exists, `meta.historyCoverageStart` is the earliest retained canonical job `first_seen_at`, or that run for an empty database. It is a local retained-history boundary, not a claim about postings TopCV had before collection began. The endpoint returns HTTP `409` while the listing lease is active, preventing an import from paginating across an in-progress mutation.

## Job fields

Identity fields are `id`, `source_job_id`, `title`, `company`, `url`, `location`, and category fields. `id` keeps the form `topcv:<source_job_id>`. Responses derive `source: "topcv"` for compatibility; the value is not stored. A response containing any other source is rejected by .NET. Normalized enrichment includes active state, salary range/currency/negotiable state, experience range, skills, requirements, benefits, specialties, detail status, and detail success time.

Posting date fields are:

| Field | Meaning |
|---|---|
| `post_date` | Best representative date; null when confidence is unknown. |
| `post_date_text` | Original source text. |
| `post_date_confidence` | `exact`, `relative`, or `unknown`. |
| `post_date_lower_bound` | Earliest date supported by current observations. |
| `post_date_upper_bound` | Latest date supported by current observations. |
| `post_date_observed_on` | Vietnam business date of the latest contributing observation. |

Unknown scalar values are JSON `null`. Known arrays are arrays, including `[]` when observed empty; unknown arrays remain `null`. Dates are ISO `YYYY-MM-DD`; timestamps are ISO 8601.

## Date invariants

- `unknown` implies `post_date = null` unless a prior reliable observation is retained by Python.
- For `exact`, lower bound, upper bound, and `post_date` are the same date.
- If both bounds exist, lower bound must be less than or equal to upper bound.
- Repeated relative observations may narrow bounds but never widen established evidence.
- .NET stores bounds as PostgreSQL `date`, not UTC timestamps.

## Health endpoint

The .NET health client reads protected endpoint:

```text
GET /api/v1/ops/health-summary
X-API-Key: <ADMIN_API_KEY>
```

It consumes `pipeline_status`, `generated_at`, `latest_listing_run`, `data_quality`, `freshness`, and `warnings`. The .NET admin endpoint returns a stable 200 envelope even when Python is down; `isAvailable=false`, `status`, and `errorMessage` carry the degraded state. Production workflow preflight is stricter and fails on an unavailable, critical, invalid, or stale source.

## Import completeness

The crawler marks a listing result complete only when it starts on page 1 and reaches empty-page end confirmation. A max-page cap, resumed suffix, lost lease, or crawler failure remains partial and cannot publish freshness or deactivate missing jobs.

An .NET import is complete only when all expected pages were fetched within configured limits with stable, unique identities. A capped, interrupted, or invalid result is partial/failed. Missing-job lifecycle changes are forbidden on a partial import, because absence from a truncated payload is not evidence that a source job disappeared.

Historical sync uses `scope=all`, upserts inactive rows as expired, and never applies the missing-job lifecycle. The publication-history watermark advances only after every page succeeds in one transaction. Legacy `active=true|false` remains accepted by Python and takes precedence over `scope`; `source=topcv` is accepted for one release and all other values return HTTP 400.

## Compatibility

Adding nullable fields is backward-compatible. Renaming/removing response fields, changing confidence values, changing null/array semantics, or changing pagination/meta fields is a breaking change and requires coordinated Python, .NET, migration, and test updates. Persisted source columns are intentionally not part of the contract.
