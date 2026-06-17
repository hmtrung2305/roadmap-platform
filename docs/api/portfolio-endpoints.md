# Portfolio Endpoint Specification

Base routes:

```text
/api/me/portfolio
/api/portfolios
```

Source controllers:

```text
MeController
PortfolioController
```

Related services / DTOs:

```text
IPortfolioService
PortfolioResponseDto
UpdatePortfolioRepositoriesRequestDto
GitHubRepositoryResponseDto
RepoInsightResponseDto
```

## Summary

Portfolio endpoints return the current user's own portfolio, update selected repositories, and expose public portfolio data by username.

The public endpoint returns `404 Not Found` when the profile is private.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/me/portfolio` | Yes | Get current user's own portfolio |
| `PATCH` | `/api/me/portfolio/repositories` | Yes | Select repositories shown on the portfolio |
| `GET` | `/api/portfolios/{username}` | No | Get public portfolio by username |

## Authentication

| Requirement | Details |
|---|---|
| Owner portfolio endpoints | Authenticated `access_token` cookie required |
| Public portfolio endpoint | Anonymous |
| User id source | `ClaimTypes.NameIdentifier` for `/api/me/portfolio` routes |

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| Repository sorting | `githubUpdatedAt DESC` in service behavior |

## Portfolio Response Shape

Used by:

| Endpoint |
|---|
| `GET /api/me/portfolio` |
| `PATCH /api/me/portfolio/repositories` |
| `GET /api/portfolios/{username}` |

### Response Body

```json
{
  "displayName": "Khoa Minh",
  "headline": "Software Engineering Student",
  "bio": "Backend and full-stack learner.",
  "location": "Vietnam",
  "avatarUrl": "https://example.com/avatar.png",
  "coverImageUrl": "https://example.com/cover.png",
  "careerGoal": "Backend Developer",
  "currentRole": "Student",
  "publicEmail": "contact@example.com",
  "githubUrl": "https://github.com/example",
  "linkedinUrl": "https://linkedin.com/in/example",
  "personalWebsiteUrl": "https://example.com",
  "repositories": [
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
}
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `displayName` | `string/null` | Public display name |
| `headline` | `string/null` | Public headline |
| `bio` | `string/null` | Public biography |
| `location` | `string/null` | Public location string |
| `avatarUrl` | `string/null` | Avatar URL |
| `coverImageUrl` | `string/null` | Cover image URL |
| `careerGoal` | `string/null` | Career goal |
| `currentRole` | `string/null` | Current role |
| `publicEmail` | `string/null` | Public contact email |
| `githubUrl` | `string/null` | GitHub profile URL |
| `linkedinUrl` | `string/null` | LinkedIn profile URL |
| `personalWebsiteUrl` | `string/null` | Personal website URL |
| `repositories` | `GitHubRepositoryResponseDto[]` | Selected public repositories |

> [!NOTE]
> `PortfolioResponseDto` does not include `isPublic`. Public visibility is controlled by profile endpoints.

## `GET /api/me/portfolio`

Returns the current authenticated user's own portfolio.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "displayName": "Khoa Minh",
  "headline": "Software Engineering Student",
  "bio": "Backend and full-stack learner.",
  "location": "Vietnam",
  "avatarUrl": null,
  "coverImageUrl": null,
  "careerGoal": "Backend Developer",
  "currentRole": "Student",
  "publicEmail": "contact@example.com",
  "githubUrl": "https://github.com/example",
  "linkedinUrl": null,
  "personalWebsiteUrl": null,
  "repositories": []
}
```

### Rules

| Rule | Behavior |
|---|---|
| User not found | Not found |
| Profile not found | Not found |
| Profile is private | Still returned because owner is requesting it |
| Success | Returns profile data and selected public repositories |

## `PATCH /api/me/portfolio/repositories`

Updates which saved repositories are displayed on the current user's portfolio.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `repositoryIds` | `guid[]` | Yes | Local repository ids owned by current user |

Example:

```json
{
  "repositoryIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
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
  "displayName": "Khoa Minh",
  "headline": "Software Engineering Student",
  "bio": "Backend and full-stack learner.",
  "location": "Vietnam",
  "avatarUrl": null,
  "coverImageUrl": null,
  "careerGoal": "Backend Developer",
  "currentRole": "Student",
  "publicEmail": "contact@example.com",
  "githubUrl": "https://github.com/example",
  "linkedinUrl": null,
  "personalWebsiteUrl": null,
  "repositories": []
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Invalid request |
| Missing `repositoryIds` | Invalid request |
| Duplicate ids | Deduplicated by service behavior |
| Repository does not belong to user | Invalid request or not found |
| Repository id not found in user's saved repositories | Invalid request or not found |
| Success | Selected ids are set to `true`; other user's repositories are set to `false` |

> [!IMPORTANT]
> This endpoint expects local `repositoryId` values, not GitHub repository ids.

## `GET /api/portfolios/{username}`

Returns a public portfolio by username.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `username` | `string` | Yes | Matched using normalized lowercase username |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "displayName": "Khoa Minh",
  "headline": "Software Engineering Student",
  "bio": "Backend and full-stack learner.",
  "location": "Vietnam",
  "avatarUrl": null,
  "coverImageUrl": null,
  "careerGoal": "Backend Developer",
  "currentRole": "Student",
  "publicEmail": "contact@example.com",
  "githubUrl": "https://github.com/example",
  "linkedinUrl": null,
  "personalWebsiteUrl": null,
  "repositories": []
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing username | Invalid request |
| Username not found | Not found |
| Profile not found | Not found |
| Profile is private | Not found |
| Success | Returns public profile and selected public repositories |

> [!WARNING]
> Private portfolios intentionally return `404 Not Found` instead of exposing that the user exists.

## Repository Visibility Rules

Repositories included in portfolio responses must satisfy:

| Condition | Required Value |
|---|---|
| Belongs to portfolio owner | `true` |
| `isSelectedForPortfolio` | `true` |
| `isPrivate` | `false` |

## Summary

Use owner portfolio endpoints for authenticated editing and the public portfolio endpoint for display by username. Repository insight may appear on repository objects because `GitHubRepositoryResponseDto` includes `insight`.
