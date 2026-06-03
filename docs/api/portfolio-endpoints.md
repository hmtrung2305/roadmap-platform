# Portfolio Endpoint Specification

Sources:

- `MeController`
- `PortfolioController`
- `PortfolioService`

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/me/portfolio` | Yes | Get current user's own portfolio |
| `PATCH` | `/api/me/portfolio/repositories` | Yes | Select repositories shown on portfolio |
| `GET` | `/api/portfolios/{username}` | No | Get public portfolio by username |

---

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
      "repositoryId": "guid",
      "name": "roadmap-platform",
      "fullName": "khoa/roadmap-platform",
      "htmlUrl": "https://github.com/khoa/roadmap-platform",
      "description": "Learning roadmap platform",
      "primaryLanguage": "C#",
      "stars": 10,
      "forks": 2,
      "isSelectedForPortfolio": true,
      "syncedAt": "2026-06-02T10:00:00Z"
    }
  ]
}
```

### Response Fields

| Field | Type |
|---|---|
| `displayName` | `string/null` |
| `headline` | `string/null` |
| `bio` | `string/null` |
| `location` | `string/null` |
| `avatarUrl` | `string/null` |
| `coverImageUrl` | `string/null` |
| `careerGoal` | `string/null` |
| `currentRole` | `string/null` |
| `publicEmail` | `string/null` |
| `githubUrl` | `string/null` |
| `linkedinUrl` | `string/null` |
| `personalWebsiteUrl` | `string/null` |
| `repositories` | `GitHubRepositoryResponseDto[]` |

> [!NOTE]
> `PortfolioResponseDto` does not include `isPublic`. `isPublic` is returned by profile endpoints, not portfolio endpoints.

---

## `GET /api/me/portfolio`

Returns the current authenticated user's portfolio.

### Rules

| Rule | Behavior |
|---|---|
| User not found | Error |
| Profile not found | Not found |
| Success | Returns profile data and selected public repositories |

> [!NOTE]
> This endpoint returns the current user's portfolio even if the profile is private.

---

## `PATCH /api/me/portfolio/repositories`

Updates which saved repositories are displayed on the current user's portfolio.

### Request Body

| Field | Type | Required | Notes |
|---|---|---:|---|
| `repositoryIds` | `guid[]` | No DTO validation, but service requires non-null | Local repository ids owned by current user |

### Example Request

```json
{
  "repositoryIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

### Success Response

Returns updated portfolio response.

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Error |
| Missing `repositoryIds` | Error |
| Duplicate ids | Deduplicated |
| Repository does not belong to user | Error |
| Repository id not found in user's saved repositories | Error |
| Success | Sets selected repositories to `true`, all other user's repositories to `false` |

> [!IMPORTANT]
> This endpoint expects local `repositoryId` values, not GitHub repository ids.

---

## `GET /api/portfolios/{username}`

Returns a public portfolio by username.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `username` | `string` | Yes | Matched using normalized lowercase username |

### Success Response

Returns portfolio response.

### Rules

| Rule | Behavior |
|---|---|
| Missing username | Error |
| Username not found | Not found |
| Profile not found | Not found |
| Profile is private | Not found |
| Success | Returns public profile and selected public repositories |

> [!CAUTION]
> Private portfolios intentionally return `404 Not Found` instead of exposing that the user exists.

---

## Repository Visibility Rules

Repositories included in portfolio responses must satisfy:

| Condition | Required Value |
|---|---|
| Belongs to portfolio owner | `true` |
| `isSelectedForPortfolio` | `true` |
| `isPrivate` | `false` |

Repositories are ordered by:

```text
githubUpdatedAt DESC
```
