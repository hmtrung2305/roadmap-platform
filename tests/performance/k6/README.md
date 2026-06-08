# Roadmap k6 Performance Tests

These tests measure the roadmap API flow used by the frontend.

## Main tested read flow

The main read test intentionally excludes:

```txt
GET /api/roadmaps/{slug}
```

because the frontend does not use the full detailed roadmap endpoint.

Instead, it tests:

```txt
GET /api/roadmaps
GET /api/roadmaps/frontend-developer/graph
GET /api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}
```

The test automatically discovers `roadmapVersionId` and a usable `roadmapNodeId` from the graph response.

## Default values

```txt
BASE_URL=https://localhost:7103
ROADMAP_SLUG=frontend-developer
LOGIN_PATH=/api/auth/login
```

## Run read test

```powershell
k6 run tests/performance/k6/roadmap/roadmap-read.test.js
```

Save before result:

```powershell
k6 run tests/performance/k6/roadmap/roadmap-read.test.js `
  --summary-export tests/performance/k6/results/before/roadmap-read-summary.json
```

Save after result:

```powershell
k6 run tests/performance/k6/roadmap/roadmap-read.test.js `
  --summary-export tests/performance/k6/results/after/roadmap-read-summary.json
```

## Run read test with authenticated cookies

Use this if the graph/node detail response includes user progress when logged in.

```powershell
$env:AUTHENTICATED_READ="true"
$env:TEST_USER="your-email-or-username"
$env:TEST_PASSWORD="your-password"

k6 run tests/performance/k6/roadmap/roadmap-read.test.js
```

## Run enrollment test

```powershell
$env:TEST_USER="your-email-or-username"
$env:TEST_PASSWORD="your-password"

k6 run tests/performance/k6/roadmap/roadmap-enrollment.test.js
```

## Run progress test

The progress endpoint needs an existing enrollment ID.

```powershell
$env:TEST_USER="your-email-or-username"
$env:TEST_PASSWORD="your-password"
$env:ROADMAP_ENROLLMENT_ID="your-enrollment-id"

k6 run tests/performance/k6/roadmap/roadmap-progress.test.js
```

## Optional manual IDs

Usually not needed for the read test because IDs are discovered from the graph response.

Set these only if auto-discovery fails:

```powershell
$env:ROADMAP_VERSION_ID="your-roadmap-version-id"
$env:ROADMAP_NODE_ID="your-node-id"
```

## Common settings

```powershell
$env:VUS="5"
$env:DURATION="30s"
$env:BASE_URL="https://localhost:7103"
$env:ROADMAP_SLUG="frontend-developer"
```

## Notes about local HTTPS

The tests default to:

```js
insecureSkipTLSVerify: true
```

This is useful for ASP.NET local HTTPS development certificates.
