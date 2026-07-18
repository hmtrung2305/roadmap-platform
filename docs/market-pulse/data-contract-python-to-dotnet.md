# Market Pulse Data Contract: Python to .NET

## Jobs endpoint

The .NET importer reads:

```text
GET /api/v1/jobs?active=true&sort=post_date_desc&page=<n>&pageSize=<n>
```

The response envelope contains `ok`, `data`, `pagination`, and `meta`. `pagination.total`, `page`, `pageSize`, and `totalPages` determine whether the import is complete. `meta.latestSuccessfulCrawlAt` is required by freshness policy.

## Job fields

Identity fields are `id`, `source`, `source_job_id`, `title`, `company`, `url`, `location`, and category fields. Normalized enrichment includes salary range/currency/negotiable state, experience range, skills, requirements, benefits, specialties, detail status, and detail success time.

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

An import is complete only when all expected active pages were fetched within configured limits. A capped or interrupted result is partial. Missing-job lifecycle changes are forbidden on a partial import, because absence from a truncated payload is not evidence that a source job disappeared.

## Compatibility

Adding nullable fields is backward-compatible. Renaming/removing fields, changing confidence values, changing null/array semantics, or changing pagination/meta fields is a breaking change and requires coordinated Python, .NET, migration, and test updates.
