# Roadmap Enrollment Endpoint Specification

Base routes:

```text
/api/roadmap-enrollments
/api/roadmap-enrollments/{roadmapEnrollmentId}/nodes
```

Source controllers:

```text
RoadmapEnrollmentsController
RoadmapProgressController
```

Related services / DTOs:

```text
IRoadmapEnrollmentService
IRoadmapProgressService
EnrollRoadmapRequestDto
RoadmapEnrollmentDto
UpdateNodeProgressRequestDto
UpdateNodeProgressResultDto
UserNodeProgressDto
```

## Summary

Roadmap enrollment endpoints let authenticated learners enroll in a published roadmap version, fetch their current enrollment, and update node progress.

Progress updates are only allowed for manual progress nodes. Container nodes and computed nodes cannot be updated directly.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `POST` | `/api/roadmap-enrollments` | Yes | Enroll current user in a published roadmap version |
| `GET` | `/api/roadmap-enrollments/current?roadmapVersionId={id}` | Yes | Get current user's enrollment for a roadmap version |
| `PATCH` | `/api/roadmap-enrollments/{roadmapEnrollmentId}/nodes/{roadmapNodeId}/progress` | Yes | Update progress for one roadmap node |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | Yes |
| Auth type | Authenticated `access_token` cookie |
| User id source | `ClaimTypes.NameIdentifier` |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| Enrollment ownership | Enrollment updates are scoped to the authenticated user |

## `POST /api/roadmap-enrollments`

Enrolls the current user in a published roadmap version.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `roadmapVersionId` | `guid` | Yes | Must reference a published roadmap version |

Example:

```json
{
  "roadmapVersionId": "22222222-2222-2222-2222-222222222222"
}
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "roadmapEnrollmentId": "11111111-1111-1111-1111-111111111111",
  "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
  "status": "active",
  "progressPercent": 0,
  "startedAt": "2026-06-16T10:00:00Z",
  "completedAt": null
}
```

### Rules

| Rule | Behavior |
|---|---|
| Empty `roadmapVersionId` | Invalid request |
| Roadmap version not published | Not found |
| User already enrolled | Existing enrollment is returned |
| New enrollment | Creates `roadmap_enrollment` with `status = active` and `progressPercent = 0` |

## `GET /api/roadmap-enrollments/current`

Returns the current user's enrollment for one roadmap version.

### Query Parameters

| Parameter | Type | Required | Default | Notes |
|---|---|---:|---|---|
| `roadmapVersionId` | `guid` | Yes | None | Roadmap version id |

Example:

```text
GET /api/roadmap-enrollments/current?roadmapVersionId=22222222-2222-2222-2222-222222222222
```

### Success Response

Status when enrollment exists:

```text
200 OK
```

Body:

```json
{
  "roadmapEnrollmentId": "11111111-1111-1111-1111-111111111111",
  "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
  "status": "active",
  "progressPercent": 24.5,
  "startedAt": "2026-06-16T10:00:00Z",
  "completedAt": null
}
```

Status when no enrollment exists:

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| Missing `roadmapVersionId` | `400 Bad Request` with `INVALID_REQUEST` |
| Enrollment exists | Returns enrollment |
| Enrollment missing | Returns `204 No Content` |

## `PATCH /api/roadmap-enrollments/{roadmapEnrollmentId}/nodes/{roadmapNodeId}/progress`

Updates manual progress for one node.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `roadmapEnrollmentId` | `guid` | Yes | Enrollment owned by current user |
| `roadmapNodeId` | `guid` | Yes | Node in the enrolled roadmap version |

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `status` | `string` | Yes | `pending`, `in_progress`, `completed`, or `skipped` |
| `evidenceUrl` | `string/null` | No | Blank becomes `null` |
| `learnerNote` | `string/null` | No | Blank becomes `null` |
| `idempotencyKey` | `string/null` | No | Prevents duplicate progress events |

Example:

```json
{
  "status": "completed",
  "evidenceUrl": "https://github.com/khoa/project",
  "learnerNote": "Finished the landing page project.",
  "idempotencyKey": "node-4444-completed-1"
}
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "enrollment": {
    "roadmapEnrollmentId": "11111111-1111-1111-1111-111111111111",
    "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
    "status": "active",
    "progressPercent": 25,
    "startedAt": "2026-06-16T10:00:00Z",
    "completedAt": null
  },
  "trackableNodeCount": 28,
  "completedNodeCount": 7,
  "progressPercent": 25,
  "changedNodes": [
    {
      "userNodeProgressId": "33333333-3333-3333-3333-333333333333",
      "roadmapEnrollmentId": "11111111-1111-1111-1111-111111111111",
      "roadmapNodeId": "44444444-4444-4444-4444-444444444444",
      "status": "completed",
      "isComputed": false,
      "evidenceUrl": "https://github.com/khoa/project",
      "learnerNote": "Finished the landing page project.",
      "startedAt": "2026-06-16T10:00:00Z",
      "completedAt": "2026-06-16T11:00:00Z",
      "skippedAt": null,
      "updatedAt": "2026-06-16T11:00:00Z"
    }
  ]
}
```

### Rules

| Rule | Behavior |
|---|---|
| Invalid `status` | Invalid request |
| Enrollment not found or not owned by current user | Not found |
| Node not found in enrolled roadmap version | Not found |
| Node has computed progress | Invalid request |
| Node currently locked | Invalid request |
| Duplicate `idempotencyKey` | Returns current computed result without creating another event |
| `pending` status | Clears started/completed/skipped timestamps |
| `in_progress` status | Sets `startedAt` if missing |
| `completed` status | Sets `startedAt` if missing and updates `completedAt` |
| `skipped` status | Sets `startedAt` if missing and updates `skippedAt` |
| All progress units completed | Enrollment status becomes `completed` |
| Not all progress units completed | Enrollment status remains or becomes `active` |

> [!IMPORTANT]
> `skipped` counts as a satisfied unit in roadmap progress, the same as `completed`.

## Progress Calculation Notes

| Topic | Behavior |
|---|---|
| Manual nodes | Usually `topic` and `project` nodes |
| Computed nodes | Container and choice group statuses are derived from children and prerequisites |
| Locked nodes | A node is locked when required prerequisites are not completed |
| Choice group completion | Uses `selectionType` and `requiredCount` |
| Progress percent | Rounded to 2 decimals |

## Implementation Notes

| Topic | Notes |
|---|---|
| Progress event | Every non-idempotent update writes a `progress_event` row |
| Idempotency | Duplicate keys are checked per enrollment |
| Ownership | Progress service queries enrollment by both enrollment id and current user id |
| Computed changes | `changedNodes` may include nodes whose effective status changed because of dependency rules |

## Summary

Enroll first, then update manual node progress through the enrollment-specific progress endpoint. Computed nodes and locked nodes are protected from direct manual updates.
