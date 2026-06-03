# GitHub Integration Endpoint Specification

Base route:

```text
/api/integrations/github
```

Source controller: `GitHubIntegrationController`

All endpoints in this file require authentication.

## Endpoint Summary

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/integrations/github/repositories` | Get saved GitHub repositories |
| `POST` | `/api/integrations/github/repositories/sync` | Sync public repositories from GitHub |

---

## `GET /api/integrations/github/repositories`

Returns repositories already saved in the local database for the current user.

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
    "syncedAt": "2026-06-02T10:00:00Z"
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

---

## `POST /api/integrations/github/repositories/sync`

Fetches the user's latest public repositories from GitHub and upserts them into the local database.

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

> [!NOTE]
> Sync does not remove local repositories that are no longer returned by GitHub. It upserts repositories that are returned by the GitHub API.
