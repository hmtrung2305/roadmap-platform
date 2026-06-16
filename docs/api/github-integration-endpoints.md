# GitHub Integration Endpoint Specification

Base route:

```text
/api/integrations/github
```

Source controller:

```text
GitHubIntegrationController
```

Related services / DTOs:

```text
IGitHubRepositoryService
IRepoInsightService
GitHubRepositoryResponseDto
RepoInsightResponseDto
```

## Summary

GitHub integration endpoints manage saved GitHub repositories and AI repository insight generation for the current user.

Repository sync fetches public repository metadata. AI insight generation is separate and runs per repository.

## Endpoint Summary

| Method | Endpoint | Auth Required | Rate Limit | Purpose |
|---|---|---:|---|---|
| `GET` | `/api/integrations/github/repositories` | Yes | Default | Get saved GitHub repositories |
| `POST` | `/api/integrations/github/repositories/sync` | Yes | `ExternalApi` | Sync public repositories from GitHub |
| `POST` | `/api/integrations/github/repositories/{repositoryId}/insight` | Yes | `AiExpensive` | Generate or refresh AI repository insight |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | Yes |
| Auth type | Authenticated `access_token` cookie |
| User id source | `User.GetUserId()` |
| GitHub token source | Linked GitHub provider access token when available |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| External API errors | May include GitHub-specific error code and `retryAfterSeconds` |

## `GET /api/integrations/github/repositories`

Returns repositories already saved in the local database for the current user.

Repositories may include an `insight` object when AI insight has already been generated.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "repositoryId": "11111111-1111-1111-1111-111111111111",
    "name": "roadmap-platform",
    "fullName": "khoa/roadmap-platform",
    "htmlUrl": "https://github.com/khoa/roadmap-platform",
    "description": "Learning roadmap platform",
    "primaryLanguage": "C#",
    "stars": 10,
    "forks": 2,
    "isSelectedForPortfolio": true,
    "syncedAt": "2026-06-16T10:00:00Z",
    "insight": {
      "insightId": "22222222-2222-2222-2222-222222222222",
      "repositoryId": "11111111-1111-1111-1111-111111111111",
      "summary": "A full-stack learning roadmap platform.",
      "techStack": ["ASP.NET Core", "PostgreSQL", "React"],
      "detectedSkills": ["REST API", "Authentication"],
      "projectType": "Full Stack Web Application",
      "analysisStatus": "completed",
      "readmeTruncated": false,
      "aiModel": "gemini-2.5-flash",
      "errorMessage": null,
      "analyzedAt": "2026-06-16T10:00:00Z",
      "updatedAt": "2026-06-16T10:00:00Z"
    }
  }
]
```

### Repository Fields

| Field | Type | Notes |
|---|---|---|
| `repositoryId` | `guid` | Local repository id |
| `name` | `string` | Repository name |
| `fullName` | `string` | GitHub owner/name |
| `htmlUrl` | `string` | GitHub repository URL |
| `description` | `string/null` | GitHub repository description |
| `primaryLanguage` | `string/null` | Main GitHub language |
| `stars` | `number` | Stargazer count |
| `forks` | `number` | Fork count |
| `isSelectedForPortfolio` | `boolean` | Whether displayed on portfolio |
| `syncedAt` | `datetime` | Last local sync time |
| `insight` | `object/null` | Latest repository insight, if available |

### Insight Fields

| Field | Type | Notes |
|---|---|---|
| `insightId` | `guid` | Local insight id |
| `repositoryId` | `guid` | Repository linked to the insight |
| `summary` | `string/null` | AI-generated summary |
| `techStack` | `string[]` | Technologies detected from README |
| `detectedSkills` | `string[]` | Skills inferred from README |
| `projectType` | `string/null` | General project category |
| `analysisStatus` | `string` | `pending`, `completed`, or `failed` |
| `readmeTruncated` | `boolean` | Whether README was shortened before analysis |
| `aiModel` | `string/null` | AI model used |
| `errorMessage` | `string/null` | Error message when analysis failed |
| `analyzedAt` | `datetime` | Last analysis time |
| `updatedAt` | `datetime` | Last insight update time |

## `POST /api/integrations/github/repositories/sync`

Fetches the user's latest public repositories from GitHub and upserts them into the local database.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "repositoryId": "11111111-1111-1111-1111-111111111111",
    "name": "roadmap-platform",
    "fullName": "khoa/roadmap-platform",
    "htmlUrl": "https://github.com/khoa/roadmap-platform",
    "description": "Learning roadmap platform",
    "primaryLanguage": "C#",
    "stars": 10,
    "forks": 2,
    "isSelectedForPortfolio": true,
    "syncedAt": "2026-06-16T10:00:00Z",
    "insight": null
  }
]
```

### Rules

| Rule | Behavior |
|---|---|
| GitHub account not linked | Error |
| GitHub username missing | Error |
| GitHub access token available | Used for API requests |
| New GitHub repo | Creates local repository row |
| Existing GitHub repo | Updates local repository fields |
| Private repos | Not synced by this flow |
| New synced repo | Defaults `isSelectedForPortfolio = true` |
| Repository insight | Not generated during sync |

> [!NOTE]
> Sync does not remove local repositories that are no longer returned by GitHub. It upserts returned repositories only.

## `POST /api/integrations/github/repositories/{repositoryId}/insight`

Generates or refreshes AI insight for one repository.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `repositoryId` | `guid` | Yes | Local repository id |

### Query Parameters

| Parameter | Type | Required | Default | Notes |
|---|---|---:|---|---|
| `force` | `boolean` | No | `false` | Regenerate even if README hash has not changed |

Example:

```text
POST /api/integrations/github/repositories/11111111-1111-1111-1111-111111111111/insight?force=true
```

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "insightId": "22222222-2222-2222-2222-222222222222",
  "repositoryId": "11111111-1111-1111-1111-111111111111",
  "summary": "A full-stack learning roadmap platform.",
  "techStack": ["ASP.NET Core", "PostgreSQL", "React"],
  "detectedSkills": ["REST API", "Authentication"],
  "projectType": "Full Stack Web Application",
  "analysisStatus": "completed",
  "readmeTruncated": false,
  "aiModel": "gemini-2.5-flash",
  "errorMessage": null,
  "analyzedAt": "2026-06-16T10:00:00Z",
  "updatedAt": "2026-06-16T10:00:00Z"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Repository does not belong to current user | Error |
| README found | README is cleaned, hashed, and analyzed |
| README missing | Insight is saved as `failed` with an error message |
| Existing insight with unchanged README hash | Existing insight is returned when `force=false` |
| `force=true` | AI summary is regenerated even when README hash is unchanged |
| AI analysis succeeds | Insight is saved as `completed` |
| AI analysis fails | Insight is saved as `failed` with an error message |

## Backend Flow

1. Load the repository by `repositoryId` and current user id.
2. Parse `fullName` into GitHub owner and repository name.
3. Fetch README from GitHub.
4. Clean and truncate README if needed.
5. Generate a SHA-256 hash from cleaned README.
6. Compare with saved `readmeHash`.
7. Skip the AI call if hash is unchanged and `force=false`.
8. Generate summary with AI.
9. Upsert the latest row in `repo_insight`.
10. Return the insight response.

## Frontend Usage

| Context | Behavior |
|---|---|
| Owner repository page | Show `Generate` when no completed insight exists |
| Owner repository page | Show `Regenerate` when completed insight exists |
| Loading state | Disable duplicate insight requests while the request is running |
| Failed insight | Show failed status and allow retry |
| Public portfolio | Prefer completed `insight.summary`; fall back to GitHub description |
| Public portfolio | Avoid exposing internal fields unless needed |

## Summary

Repository sync and repository insight generation are intentionally separate. Sync stays fast; AI insight generation is explicit and repository-specific.
