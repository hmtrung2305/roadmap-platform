# GitHub Integration Endpoint Specification

Base route:

```text
/api/integrations/github
```

Source controller: `GitHubIntegrationController`

All endpoints in this file require authentication.

Repository insight belongs in this file because it is part of the GitHub repository integration flow. It uses the same base route, controller, repository records, and authentication rules.

## Endpoint Summary

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/integrations/github/repositories` | Get saved GitHub repositories |
| `POST` | `/api/integrations/github/repositories/sync` | Sync public repositories from GitHub |
| `POST` | `/api/integrations/github/repositories/{repositoryId}/insight` | Generate or refresh an AI repository insight |

## `GET /api/integrations/github/repositories`

Returns repositories already saved in the local database for the current user.

Repositories may include an `insight` object when an AI summary has already been generated.

### Success Response

```json
[
  {
    "repositoryId": "guid",
    "name": "roadmap-platform",
    "fullName": "khoa/roadmap-platform",
    "htmlUrl": "https://github.com/khoa/roadmap-platform",
    "description": "Learning roadmap platform",
    "primaryLanguage": "C#",
    "stars": 10,
    "forks": 2,
    "isSelectedForPortfolio": true,
    "syncedAt": "2026-06-02T10:00:00Z",
    "insight": {
      "insightId": "guid",
      "repositoryId": "guid",
      "summary": "A full-stack learning roadmap platform with portfolio, authentication, and roadmap progress features.",
      "techStack": ["ASP.NET Core", "PostgreSQL", "React"],
      "detectedSkills": ["REST API", "Authentication", "Database Design"],
      "projectType": "Full Stack Web Application",
      "analysisStatus": "completed",
      "readmeTruncated": false,
      "aiModel": "gemini-2.5-flash",
      "errorMessage": null,
      "analyzedAt": "2026-06-09T10:00:00Z",
      "updatedAt": "2026-06-09T10:00:00Z"
    }
  }
]
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `repositoryId` | `guid` | Local repository id |
| `name` | `string` | Repository name |
| `fullName` | `string` | GitHub owner/name |
| `htmlUrl` | `string` | GitHub repository URL |
| `description` | `string/null` | Repository description |
| `primaryLanguage` | `string/null` | Main GitHub language |
| `stars` | `number` | Stargazer count |
| `forks` | `number` | Fork count |
| `isSelectedForPortfolio` | `boolean` | Whether displayed on portfolio |
| `syncedAt` | `datetime` | Last local sync time |
| `insight` | `object/null` | Latest generated repository insight, if available |

### Insight Fields

| Field | Type | Notes |
|---|---|---|
| `insightId` | `guid` | Local insight id |
| `repositoryId` | `guid` | Repository linked to the insight |
| `summary` | `string/null` | AI-generated repository summary |
| `techStack` | `array` | Technologies detected from the README |
| `detectedSkills` | `array` | Skills inferred from the README |
| `projectType` | `string/null` | General project category |
| `analysisStatus` | `string` | `pending`, `completed`, or `failed` |
| `readmeTruncated` | `boolean` | Whether README content was shortened before analysis |
| `aiModel` | `string/null` | AI model used for the summary |
| `errorMessage` | `string/null` | Internal analysis error message, if analysis failed |
| `analyzedAt` | `datetime` | Last analysis time |
| `updatedAt` | `datetime` | Last insight update time |

## `POST /api/integrations/github/repositories/sync`

Fetches the user's latest public repositories from GitHub and upserts them into the local database.

This endpoint only syncs repository metadata. It does not generate AI insights.

### Success Response

Returns the saved repository list after sync.

### Rules

| Rule | Behavior |
|---|---|
| GitHub account not linked | Error |
| GitHub username missing | Error |
| New GitHub repo | Creates local repository row |
| Existing GitHub repo | Updates local repository fields |
| Private repos | Stored as `isPrivate = false` because only public repos are synced |
| New synced repo | Defaults `isSelectedForPortfolio = true` |
| Repository insight | Not generated during sync |

> [!NOTE]
> Sync does not remove local repositories that are no longer returned by GitHub. It upserts repositories that are returned by the GitHub API.

## `POST /api/integrations/github/repositories/{repositoryId}/insight`

Generates or refreshes an AI insight for one repository.

The endpoint reads the repository README from GitHub, cleans and hashes the content, then uses AI to generate a portfolio-friendly summary.

### Query Parameters

| Parameter | Type | Required | Notes |
|---|---|---|---|
| `force` | `boolean` | No | When `true`, regenerates the insight even if the README hash has not changed |

### Success Response

```json
{
  "insightId": "guid",
  "repositoryId": "guid",
  "summary": "A full-stack learning roadmap platform with portfolio, authentication, and roadmap progress features.",
  "techStack": ["ASP.NET Core", "PostgreSQL", "React"],
  "detectedSkills": ["REST API", "Authentication", "Database Design"],
  "projectType": "Full Stack Web Application",
  "analysisStatus": "completed",
  "readmeTruncated": false,
  "aiModel": "gemini-2.5-flash",
  "errorMessage": null,
  "analyzedAt": "2026-06-09T10:00:00Z",
  "updatedAt": "2026-06-09T10:00:00Z"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Repository does not belong to current user | Error |
| README found | README is cleaned, hashed, and analyzed |
| README missing | Insight is saved as `failed` with an error message |
| Existing insight with unchanged README hash | Existing insight is returned without a new AI call |
| `force=true` | AI summary is regenerated even when README hash is unchanged |
| AI analysis succeeds | Insight is saved as `completed` |
| AI analysis fails | Insight is saved as `failed` with an error message |

### Backend Flow

1. Load the repository by `repositoryId` and current user id.
2. Parse `fullName` into GitHub owner and repository name.
3. Fetch the repository README from GitHub.
4. Clean and truncate the README if needed.
5. Generate a SHA-256 hash from the cleaned README.
6. Compare the hash with the saved `readmeHash`.
7. Skip the AI call if the hash is unchanged and `force` is not enabled.
8. Generate the summary with AI.
9. Upsert the latest row in `repo_insight`.
10. Return the generated insight.

> [!IMPORTANT]
> Repository sync and repository insight generation are separate. Sync should stay fast and should not trigger AI calls automatically.

## Portfolio Usage

Repository insight improves the portfolio display, but repositories still work without it.

For owner-facing repository management pages:

- Show `Generate` when no completed insight exists.
- Show `Regenerate` when a completed insight exists.
- Show loading state while the insight request is running.
- Show failed status when analysis fails.

For public portfolio pages:

- Prefer `insight.summary` when `analysisStatus` is `completed`.
- Fall back to the GitHub repository `description` when no completed insight exists.
- Show `techStack`, `detectedSkills`, and `projectType` only when available.
- Do not expose internal fields such as `readmeHash`, `aiModel`, or `errorMessage` publicly unless explicitly needed.

## Summary

Repository insight should be documented inside this GitHub integration endpoint file because it is part of the same repository workflow.

The main flow is:

```text
Sync repositories
→ choose portfolio repositories
→ generate AI insight per repository
→ display completed insight on the portfolio
```
