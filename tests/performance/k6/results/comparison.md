# Roadmap Endpoint Performance Comparison

## Test Setup

- Tool: k6
- Backend base URL: `https://localhost:7103`
- Roadmap slug: `frontend-developer`
- Duration: same for before and after
- Virtual users: same for before and after
- Environment: same machine, same database, same API configuration

## Main Read Flow Tested

- `GET /api/roadmaps`
- `GET /api/roadmaps/frontend-developer/graph`
- `GET /api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}`

The full detail endpoint `GET /api/roadmaps/{slug}` is intentionally excluded because the frontend uses the graph endpoint first and lazy-loads node details.

## Results

| Metric | Before | After | Change |
|---|---:|---:|---:|
| Avg response time | TODO | TODO | TODO |
| p95 response time | TODO | TODO | TODO |
| Failure rate | TODO | TODO | TODO |
| Requests/sec | TODO | TODO | TODO |
| Data received | TODO | TODO | TODO |
| Data sent | TODO | TODO | TODO |

## Notes

Before optimization:

- TODO

After optimization:

- TODO

## Conclusion

TODO
