# Roadmap Endpoint Performance Comparison

## Purpose

This document compares the k6 roadmap endpoint performance results before and after backend optimization.

The comparison is based on the zipped result folders:

- `before.zip`
- `after.zip`

## Test Setup

| Item | Value |
|---|---|
| Tool | k6 |
| Backend | `https://localhost:7103` |
| Roadmap slug | `frontend-developer` |
| Read test load | 5 VUs for 30s |
| Enrollment test load | 3 VUs for 30s |
| Progress test load | 3 VUs for 30s |
| Environment | Local backend and local database |

## Flows Tested

| Test | Script | Flow |
|---|---|---|
| Read flow | `roadmap-read.test.js` | `GET /api/roadmaps`, `GET /api/roadmaps/frontend-developer/graph`, `GET /api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}` |
| Enrollment flow | `roadmap-enrollment.test.js` | Login, graph setup, current enrollment check, and enrollment request. |
| Progress flow | `roadmap-progress.test.js` | Login, graph setup, update node to `in_progress`, then update node to `completed`. |

## Change Formula

Percentage change is calculated as:

$$
\text{Change} = \frac{\text{After} - \text{Before}}{\text{Before}} \times 100
$$

For latency and payload metrics, lower is better.

For throughput metrics such as requests/sec and iterations/sec, higher is better.

## Execution Summary

| Test | Before VUs | After VUs | Before Iterations | After Iterations | Before Requests | After Requests |
|---|---:|---:|---:|---:|---:|---:|
| Roadmap Read Flow | 5 | 5 | 77 | 145 | 232 | 436 |
| Roadmap Enrollment Flow | 3 | 3 | 90 | 90 | 182 | 182 |
| Roadmap Progress Flow | 3 | 3 | 90 | 90 | 182 | 182 |

## Overall Result Summary

| Test | Result | Summary |
|---|---|---|
| Read flow | Strong improvement | Average latency decreased by 94.31%, p95 latency decreased by 95.54%, requests/sec increased by 94.97%, and iterations/sec increased by 95.37%. |
| Enrollment flow | Mixed but mostly stable | Average latency improved by 34.31%, max latency improved by 66.23%, but p95 latency changed by +23.46%. |
| Progress flow | Slight regression | Average latency changed by +5.84%, p95 latency changed by +12.07%, but max latency improved by 64.96%. |

## Read Flow Metrics

| Metric | Before | After | Change | Result |
|---|---:|---:|---:|---|
| Average response time | 338.82 ms | 19.29 ms | -94.31% | Improved |
| Median response time | 35.85 ms | 8.82 ms | -75.39% | Improved |
| p90 response time | 994.49 ms | 42.06 ms | -95.77% | Improved |
| p95 response time | 1052.66 ms | 46.98 ms | -95.54% | Improved |
| Max response time | 1755.34 ms | 178.54 ms | -89.83% | Improved |
| Request failure rate | 0.00% | 0.00% | N/A | N/A |
| Requests/sec | 7.21/s | 14.05/s | +94.97% | Improved |
| Iterations/sec | 2.39/s | 4.67/s | +95.37% | Improved |
| Data received | 15.43 MB | 28.87 MB | +87.17% | Regressed |
| Data sent | 69.42 KB | 123.05 KB | +77.24% | Regressed |

## Read Flow Interpretation

The read flow shows the clearest optimization win.

Average response time improved from `338.82 ms` to `19.29 ms`, a `94.31%` reduction.

p95 response time improved from `1052.66 ms` to `46.98 ms`, a `95.54%` reduction.

Requests/sec increased from `7.21/s` to `14.05/s`, a `94.97%` increase.

Iterations/sec increased from `2.39/s` to `4.67/s`, a `95.37%` increase.

Data received increased from `15.43 MB` to `28.87 MB` because the optimized run completed more requests in the same test window. The stronger indicators are latency and throughput, which both improved significantly.

## Enrollment Flow Metrics

| Metric | Before | After | Change | Result |
|---|---:|---:|---:|---|
| Average response time | 5.87 ms | 3.86 ms | -34.31% | Improved |
| Median response time | 1.76 ms | 1.81 ms | +2.98% | Regressed |
| p90 response time | 2.24 ms | 2.51 ms | +12.16% | Regressed |
| p95 response time | 2.95 ms | 3.64 ms | +23.46% | Regressed |
| Max response time | 686.53 ms | 231.85 ms | -66.23% | Improved |
| Request failure rate | 0.00% | 0.00% | N/A | N/A |
| Requests/sec | 5.89/s | 5.98/s | +1.47% | Improved |
| Iterations/sec | 2.91/s | 2.96/s | +1.47% | Improved |
| Data received | 247.07 KB | 247.04 KB | -0.01% | Improved |
| Data sent | 24.18 KB | 24.04 KB | -0.58% | Improved |

## Enrollment Flow Interpretation

The enrollment flow is mixed but mostly stable.

Average response time improved from `5.87 ms` to `3.86 ms`, a `34.31%` reduction.

The max response time improved from `686.53 ms` to `231.85 ms`, a `66.23%` reduction.

p95 response time changed from `2.95 ms` to `3.64 ms`, which is a `+23.46%` change.

The request count stayed the same, and the request failure rate stayed at `0.00%`.

## Progress Flow Metrics

| Metric | Before | After | Change | Result |
|---|---:|---:|---:|---|
| Average response time | 12.68 ms | 13.42 ms | +5.84% | Regressed |
| Median response time | 7.70 ms | 8.71 ms | +13.21% | Regressed |
| p90 response time | 11.29 ms | 12.68 ms | +12.25% | Regressed |
| p95 response time | 12.07 ms | 13.52 ms | +12.07% | Regressed |
| Max response time | 650.35 ms | 227.87 ms | -64.96% | Improved |
| Request failure rate | 0.00% | 0.00% | N/A | N/A |
| Requests/sec | 5.82/s | 5.89/s | +1.22% | Improved |
| Iterations/sec | 2.88/s | 2.91/s | +1.22% | Improved |
| Data received | 333.63 KB | 333.69 KB | +0.02% | Regressed |
| Data sent | 26.64 KB | 26.41 KB | -0.89% | Improved |

## Progress Flow Interpretation

The progress flow shows a small latency regression in normal response-time metrics.

Average response time changed from `12.68 ms` to `13.42 ms`, which is a `+5.84%` change.

p95 response time changed from `12.07 ms` to `13.52 ms`, which is a `+12.07%` change.

The max response time improved from `650.35 ms` to `227.87 ms`, a `64.96%` reduction.

Overall, the progress endpoint is still fast, but the after run was slightly slower for typical requests.

## Reliability

| Test | Before Check Failures | After Check Failures | Before Failure Rate | After Failure Rate |
|---|---:|---:|---:|---:|
| Read flow | 0 | 0 | 0.00% | 0.00% |
| Enrollment flow | 0 | 0 | 0.00% | 0.00% |
| Progress flow | 0 | 0 | 0.00% | 0.00% |

All three flows completed with `0.00%` request failure rate and no check failures.

## Key Findings

- The roadmap read flow improved significantly after optimization.
- The read flow average response time decreased by `94.31%`.
- The read flow p95 response time decreased by `95.54%`.
- The read flow requests/sec increased by `94.97%`.
- The read flow iterations/sec increased by `95.37%`.
- The enrollment flow stayed stable, with average latency improving by `34.31%`.
- The progress flow remained fast, but average latency changed by `+5.84%` and p95 latency changed by `+12.07%`.
- Reliability stayed stable across all tested flows.

## Conclusion

The optimization was successful for the primary roadmap read flow.

The main frontend loading path improved strongly: average latency decreased by `94.31%`, p95 latency decreased by `95.54%`, requests/sec increased by `94.97%`, and iterations/sec increased by `95.37%`.

This is the most important result because the read flow represents the frontend roadmap loading path: roadmap list, roadmap graph, and lazy-loaded node detail.

Enrollment remained stable, and progress updates stayed fast enough for user interaction, although the progress endpoint should be watched in later performance passes.
