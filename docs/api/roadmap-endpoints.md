# Roadmap Endpoint Specification

Base route:

```text
/api/roadmaps
```

Source controller:

```text
RoadmapsController
```

Related services / DTOs:

```text
IRoadmapQueryService
RoadmapSummaryDto
RoadmapDetailDto
RoadmapGraphDto
RoadmapVersionUpdateDto
RoadmapVersionHistoryItemDto
RoadmapNodeDetailDto
RoadmapLearningModuleDto
```

## Summary

Roadmap endpoints expose published public roadmap data.

The list endpoint returns summary cards. The graph endpoint returns lightweight graph data for the roadmap viewer. Node detail is lazy-loaded separately and includes skills, learning resources, and published learning modules mapped to the node's skills. Detail and graph responses include `versionHistory` so learners can view changelog entries across published roadmap versions.

Authenticated users may receive enrollment, progress data, and update notices in roadmap responses. Anonymous users can still view published public roadmap data.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/roadmaps` | No | List published public roadmaps |
| `GET` | `/api/roadmaps/{slug}` | No | Get full published roadmap detail by career role slug |
| `GET` | `/api/roadmaps/{slug}/graph` | No | Get lightweight graph data by career role slug |
| `GET` | `/api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}` | No | Get lazy-loaded node detail |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | No |
| Optional auth behavior | If authenticated, `ClaimTypes.NameIdentifier` is used to include enrollment progress |
| Anonymous behavior | Returns roadmap data with `enrollment = null`, `availableUpdate = null`, computed default progress, and public `versionHistory` |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| Published filter | Only `roadmap_version.status = published` and `roadmap.visibility = public` are returned by slug/list endpoints |

## `GET /api/roadmaps`

Returns published public roadmap summaries.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "roadmapId": "11111111-1111-1111-1111-111111111111",
    "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
    "slug": "frontend-developer",
    "title": "Frontend Developer Roadmap",
    "description": "A structured roadmap for frontend development.",
    "roadmapType": "career_role",
    "sourceType": "static",
    "visibility": "public",
    "versionNumber": 1,
    "majorVersion": 1,
    "minorVersion": 0,
    "patchVersion": 0,
    "versionLabel": "v1.0.0",
    "releaseType": "major",
    "createdFromVersionId": null,
    "estimatedTotalHours": 400,
    "estimatedRequiredHours": 320.0,
    "estimatedOptionalHours": 80.0,
    "generationStatus": "published",
    "layoutDirection": "TB",
    "layoutAlgorithm": "seeded_manual",
    "nodeCount": 42,
    "careerRole": {
      "careerRoleId": "33333333-3333-3333-3333-333333333333",
      "name": "Frontend Developer",
      "slug": "frontend-developer",
      "description": "Build client-side web applications.",
      "category": "Software Development"
    }
  }
]
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `roadmapId` | `guid` | Roadmap id |
| `roadmapVersionId` | `guid` | Published version id |
| `slug` | `string` | Career role slug used for public routes |
| `title` | `string` | Published roadmap version title |
| `description` | `string/null` | Version description fallback to roadmap description |
| `roadmapType` | `string` | Roadmap category/type |
| `sourceType` | `string` | Example: `static` |
| `visibility` | `string` | Public roadmaps only for this endpoint |
| `versionNumber` | `number` | Internal monotonic version number |
| `majorVersion` | `number` | Semantic major version |
| `minorVersion` | `number` | Semantic minor version |
| `patchVersion` | `number` | Semantic patch version |
| `versionLabel` | `string` | Display label, for example `v1.0.0` |
| `releaseType` | `string` | `major`, `minor`, or `patch` |
| `createdFromVersionId` | `guid/null` | Source version id for update drafts and derived versions |
| `estimatedTotalHours` | `number/null` | Seeded total hours |
| `estimatedRequiredHours` | `number` | Calculated required hours |
| `estimatedOptionalHours` | `number` | Calculated optional hours |
| `generationStatus` | `string` | Version generation status |
| `layoutDirection` | `string` | Example: `TB` |
| `layoutAlgorithm` | `string/null` | Example: `seeded_manual` |
| `nodeCount` | `number` | Number of nodes in the published version |
| `careerRole` | `object` | Career role summary |

### Rules

| Rule | Behavior |
|---|---|
| Multiple published versions exist | Latest `versionNumber` per roadmap is returned |
| Roadmap not public | Excluded |
| Version not published | Excluded |
| Sorting | Ordered by career role name, then roadmap title |

## `GET /api/roadmaps/{slug}`

Returns full published roadmap detail by career role slug.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `slug` | `string` | Yes | Career role slug; normalized to lowercase in service lookup |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "roadmapId": "11111111-1111-1111-1111-111111111111",
  "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
  "slug": "frontend-developer",
  "title": "Frontend Developer Roadmap",
  "description": "A structured roadmap for frontend development.",
  "roadmapType": "career_role",
  "sourceType": "static",
  "visibility": "public",
  "versionNumber": 1,
  "majorVersion": 1,
  "minorVersion": 0,
  "patchVersion": 0,
  "versionLabel": "v1.0.0",
  "releaseType": "major",
  "createdFromVersionId": null,
  "estimatedTotalHours": 400,
  "estimatedRequiredHours": 320.0,
  "estimatedOptionalHours": 80.0,
  "generationStatus": "published",
  "generationModel": null,
  "generationError": null,
  "layoutDirection": "TB",
  "layoutAlgorithm": "seeded_manual",
  "careerRole": {
    "careerRoleId": "33333333-3333-3333-3333-333333333333",
    "name": "Frontend Developer",
    "slug": "frontend-developer",
    "description": "Build client-side web applications.",
    "category": "Software Development"
  },
  "enrollment": null,
  "availableUpdate": null,
  "versionHistory": [
    {
      "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
      "versionNumber": 1,
      "majorVersion": 1,
      "minorVersion": 0,
      "patchVersion": 0,
      "versionLabel": "v1.0.0",
      "releaseType": "major",
      "status": "published",
      "title": "Frontend Developer Roadmap",
      "description": "A structured roadmap for frontend development.",
      "changeLog": "Initial published roadmap.",
      "createdAt": "2026-07-01T07:00:00Z",
      "publishedAt": "2026-07-01T07:30:00Z"
    }
  ],
  "trackableNodeCount": 28,
  "completedNodeCount": 0,
  "progressPercent": 0,
  "nodes": [],
  "edges": []
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing or blank slug | Invalid request |
| Slug not found | Not found |
| Roadmap private or version unpublished | Not found |
| Authenticated user enrolled | Includes `enrollment` and saved/computed progress |
| Authenticated user enrolled on an older version | Latest published minor/major response includes `availableUpdate` so the learner can migrate manually |
| Anonymous user | `enrollment = null` |
| Version history | Includes published and archived versions for the same roadmap, newest first |
| Full detail payload | Includes full node descriptions, resources, skills, outcomes, criteria, and edges |

> [!NOTE]
> For the roadmap viewer, prefer `/graph` for initial load and fetch node detail lazily with `/nodes/{roadmapNodeId}`.

## `GET /api/roadmaps/{slug}/graph`

Returns lightweight graph data by career role slug.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `slug` | `string` | Yes | Career role slug |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "roadmapId": "11111111-1111-1111-1111-111111111111",
  "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
  "slug": "frontend-developer",
  "title": "Frontend Developer Roadmap",
  "description": "A structured roadmap for frontend development.",
  "roadmapType": "career_role",
  "sourceType": "static",
  "visibility": "public",
  "versionNumber": 1,
  "majorVersion": 1,
  "minorVersion": 0,
  "patchVersion": 0,
  "versionLabel": "v1.0.0",
  "releaseType": "major",
  "createdFromVersionId": null,
  "estimatedTotalHours": 400,
  "estimatedRequiredHours": 320.0,
  "estimatedOptionalHours": 80.0,
  "generationStatus": "published",
  "layoutDirection": "TB",
  "layoutAlgorithm": "seeded_manual",
  "careerRole": {
    "careerRoleId": "33333333-3333-3333-3333-333333333333",
    "name": "Frontend Developer",
    "slug": "frontend-developer",
    "description": "Build client-side web applications.",
    "category": "Software Development"
  },
  "enrollment": null,
  "availableUpdate": {
    "roadmapEnrollmentId": "99999999-9999-9999-9999-999999999999",
    "currentRoadmapVersionId": "11111111-1111-1111-1111-111111111111",
    "currentVersionLabel": "v1.0.0",
    "targetRoadmapVersionId": "22222222-2222-2222-2222-222222222222",
    "targetVersionLabel": "v1.1.0",
    "releaseType": "minor",
    "title": "Frontend Developer Roadmap",
    "description": "A structured roadmap for frontend development.",
    "publishedAt": "2026-07-01T07:30:00Z",
    "progressPercent": 24.5
  },
  "versionHistory": [
    {
      "roadmapVersionId": "22222222-2222-2222-2222-222222222222",
      "versionNumber": 2,
      "majorVersion": 1,
      "minorVersion": 1,
      "patchVersion": 0,
      "versionLabel": "v1.1.0",
      "releaseType": "minor",
      "status": "published",
      "title": "Frontend Developer Roadmap",
      "description": "Added deployment and testing guidance.",
      "changeLog": "Added deployment checklist and refreshed testing nodes.",
      "createdAt": "2026-07-01T07:00:00Z",
      "publishedAt": "2026-07-01T07:30:00Z"
    }
  ],
  "trackableNodeCount": 28,
  "completedNodeCount": 0,
  "progressPercent": 0,
  "nodes": [
    {
      "roadmapNodeId": "44444444-4444-4444-4444-444444444444",
      "parentNodeId": null,
      "slug": "html-css-basics",
      "nodeType": "topic",
      "checkpointType": null,
      "selectionType": null,
      "requiredCount": null,
      "title": "HTML and CSS Basics",
      "orderIndex": 1,
      "layoutRole": "leaf",
      "layoutGroup": "frontend-foundations",
      "layoutRank": 1,
      "layoutOrder": 1,
      "estimatedRequiredHours": 12.0,
      "estimatedOptionalHours": 0.0,
      "positionX": 120.0,
      "positionY": 240.0,
      "isRequired": true,
      "isTrackable": true,
      "progress": {
        "userNodeProgressId": null,
        "roadmapEnrollmentId": null,
        "roadmapNodeId": "44444444-4444-4444-4444-444444444444",
        "status": "pending",
        "isComputed": true,
        "evidenceUrl": null,
        "learnerNote": null,
        "startedAt": null,
        "completedAt": null,
        "skippedAt": null,
        "updatedAt": null
      }
    }
  ],
  "edges": [
    {
      "roadmapEdgeId": "55555555-5555-5555-5555-555555555555",
      "fromNodeId": "44444444-4444-4444-4444-444444444444",
      "toNodeId": "66666666-6666-6666-6666-666666666666",
      "edgeType": "dependency",
      "dependencyType": "required"
    }
  ]
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing or blank slug | Invalid request |
| Slug not found | Not found |
| Response size | Omits long node body fields such as descriptions, resources, skills, outcomes, and criteria |
| Progress | Includes computed progress for every graph node |
| Frontend use | Intended for ReactFlow graph rendering |
| Version history | Includes published and archived versions for changelog display |

## `versionHistory`

`versionHistory` appears on both full detail and graph responses. It is public roadmap metadata and is visible to learners.

| Field | Type | Notes |
|---|---|---|
| `roadmapVersionId` | `guid` | Version id |
| `versionNumber` | `number` | Internal monotonic version number |
| `majorVersion` | `number` | Semantic major version |
| `minorVersion` | `number` | Semantic minor version |
| `patchVersion` | `number` | Semantic patch version |
| `versionLabel` | `string` | Display label, for example `v1.1.0` |
| `releaseType` | `string` | `major`, `minor`, or `patch` |
| `status` | `string` | `published` or `archived` |
| `title` | `string` | Version title |
| `description` | `string/null` | Version description |
| `changeLog` | `string/null` | Latest `submitted` review event message for the version |
| `createdAt` | `datetime` | Version creation timestamp |
| `publishedAt` | `datetime/null` | Version publish timestamp |

Rules:

| Rule | Behavior |
|---|---|
| Included statuses | `published` and `archived` |
| Sorting | Newest semantic version first |
| Changelog source | Latest `roadmap_version_review_event` with `event_type = submitted` |
| Missing submitted event | `changeLog = null`; clients may fall back to `description` |
| Learner UI | Shows current learning version, latest roadmap version, and changelog by version |

## `availableUpdate`

`availableUpdate` can appear on both full detail and graph responses.

| Field | Type | Notes |
|---|---|---|
| `roadmapEnrollmentId` | `guid` | Current user's existing enrollment on an older version |
| `currentRoadmapVersionId` | `guid` | Version the learner is currently enrolled in |
| `currentVersionLabel` | `string` | Example: `v1.0.0` |
| `targetRoadmapVersionId` | `guid` | Latest published minor/major version the learner can migrate to |
| `targetVersionLabel` | `string` | Example: `v1.1.0` |
| `releaseType` | `string` | `minor` or `major` |
| `title` | `string` | Target version title |
| `description` | `string/null` | Target version description |
| `publishedAt` | `datetime/null` | Target version publish time |
| `progressPercent` | `number` | Progress percent from the learner's current enrollment |

Rules:

| Rule | Behavior |
|---|---|
| Anonymous user | `availableUpdate = null` |
| User already enrolled in latest version | `availableUpdate = null` |
| Latest release is `patch` | No manual update notice; patch enrollments are remapped automatically after approval |
| User enrolled in an older version and latest release is `minor` or `major` | Returns `availableUpdate` |

Use `POST /api/roadmap-enrollments/{roadmapEnrollmentId}/migrate` to apply the update.

## `GET /api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}`

Returns detailed data for one roadmap node.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `roadmapVersionId` | `guid` | Yes | Roadmap version id |
| `roadmapNodeId` | `guid` | Yes | Node id in the roadmap version |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "roadmapNodeId": "44444444-4444-4444-4444-444444444444",
  "parentNodeId": null,
  "slug": "html-css-basics",
  "nodeType": "topic",
  "checkpointType": null,
  "selectionType": null,
  "requiredCount": null,
  "title": "HTML and CSS Basics",
  "description": "Learn document structure and styling fundamentals.",
  "reason": "These skills are required before building UI components.",
  "estimatedHours": 12,
  "estimatedRequiredHours": 12.0,
  "estimatedOptionalHours": 0.0,
  "difficultyLevel": "beginner",
  "priority": 1,
  "metadata": null,
  "isRequired": true,
  "isTrackable": true,
  "learningOutcomes": ["Build semantic HTML pages"],
  "completionCriteria": ["Complete a static landing page"],
  "skills": [
    {
      "skillId": "77777777-7777-7777-7777-777777777777",
      "name": "CSS",
      "slug": "css",
      "category": "Frontend"
    }
  ],
  "resources": [
    {
      "resourceId": "88888888-8888-8888-8888-888888888888",
      "title": "MDN CSS Basics",
      "url": "https://developer.mozilla.org/",
      "resourceType": "documentation",
      "description": "CSS documentation.",
      "provider": "MDN",
      "difficultyLevel": "beginner",
      "languageCode": "en"
    }
  ],
  "learningModules": [
    {
      "skillModuleId": "99999999-9999-9999-9999-999999999999",
      "skillId": "77777777-7777-7777-7777-777777777777",
      "title": "CSS Fundamentals",
      "slug": "css-fundamentals",
      "difficultyLevel": "beginner",
      "estimatedHours": 4.5,
      "lessonCount": 3,
      "questionCount": 10,
      "provider": "Roadmap Platform"
    }
  ],
  "children": [],
  "progress": {
    "userNodeProgressId": null,
    "roadmapEnrollmentId": null,
    "roadmapNodeId": "44444444-4444-4444-4444-444444444444",
    "status": "pending",
    "isComputed": true,
    "evidenceUrl": null,
    "learnerNote": null,
    "startedAt": null,
    "completedAt": null,
    "skippedAt": null,
    "updatedAt": null
  }
}
```

### Rules

| Rule | Behavior |
|---|---|
| Empty `roadmapVersionId` | Invalid request |
| Empty `roadmapNodeId` | Invalid request |
| Node not found in version | Not found |
| Authenticated user enrolled | Includes saved/computed node progress |
| Learning modules | Only published modules mapped to the node's skills are returned |
| Computed nodes | `isComputed = true` when status is derived from child/prerequisite rules |

## Shared Status Values

### Node Progress Status

| Status | Meaning |
|---|---|
| `pending` | Not started and not locked |
| `locked` | Computed status when required prerequisites are not completed |
| `in_progress` | Started or partly satisfied |
| `completed` | Completed or computed as complete |
| `skipped` | Manually skipped and counted as a satisfied unit |

### Node Types

| Node type | Notes |
|---|---|
| `phase` | Container / backbone node |
| `topic` | Trackable learning node |
| `project` | Trackable applied node |
| `choice_group` | Computed node with child selection rules |

### Edge Types

| Edge type | Notes |
|---|---|
| `sequence` | Roadmap flow ordering |
| `dependency` | Progress prerequisite |
| `unlock` | Unlock relationship |

## Implementation Notes

| Topic | Notes |
|---|---|
| List query | Selects latest published version per roadmap |
| Graph query | Uses a lightweight projection to avoid loading all detail fields |
| Node detail | Loads skills, learning resources, and published learning modules |
| Progress calculation | Computed from nodes, edges, and saved user progress |
| Progress formula | `completedUnits / totalUnits * 100`, rounded to 2 decimals |

## Summary

Use `/api/roadmaps` for listing, `/api/roadmaps/{slug}/graph` for the roadmap canvas, and `/api/roadmaps/{roadmapVersionId}/nodes/{roadmapNodeId}` for side-panel details.
