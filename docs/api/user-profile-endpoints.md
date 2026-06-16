# Current User and Profile Endpoint Specification

Base route:

```text
/api/me
```

Source controller:

```text
MeController
```

Related services / DTOs:

```text
IUserService
UpdateCurrentUserRequestDto
UserResponseDto
AuthenticatedUserDto
UpdateProfileRequestDto
ProfileResponseDto
```

## Summary

These endpoints manage the authenticated user's account record and profile details.

The account endpoint returns account-level data such as username, email, status, and roles. Profile endpoints return public-facing profile fields used by the portfolio.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/me` | Yes | Get current user account |
| `PATCH` | `/api/me` | Yes | Update current username |
| `DELETE` | `/api/me` | Yes | Soft-delete current account |
| `GET` | `/api/me/profile` | Yes | Get current user's profile |
| `PATCH` | `/api/me/profile` | Yes | Update current user's profile |

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
| Date format | ISO 8601 datetime string when dates are returned |
| Error format | Shared `ApiErrorResponse` object |

## `GET /api/me`

Returns the current authenticated user.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "username": "khoa",
  "email": "user@example.com",
  "status": "active",
  "roles": ["learner"]
}
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | `guid` | Current user id |
| `username` | `string` | Account username |
| `email` | `string/null` | Preferred email from linked auth providers |
| `status` | `string` | Example: `active`, `deleted`, `suspended` |
| `roles` | `string[]` | User role names |

### Rules

| Rule | Behavior |
|---|---|
| Invalid user id claim | Unauthorized |
| User not found | Not found |
| Success | Returns authenticated account data |

## `PATCH /api/me`

Updates the current username.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `username` | `string` | Yes | 3-40 chars; letters, numbers, `.`, `_`, `-` only |

Example:

```json
{
  "username": "new_username"
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
  "userId": "11111111-1111-1111-1111-111111111111",
  "username": "new_username",
  "email": "user@example.com",
  "status": "active"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Validation or invalid request error |
| Missing username | Validation error |
| Duplicate username | Conflict |
| Success | Updates `username`, `usernameNormalized`, and `updatedAt` |

## `DELETE /api/me`

Soft-deletes the current account.

### Success Response

Status:

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| User not found | Not found |
| Account already deleted | Conflict |
| Success | Sets status to `deleted`, sets `deletedAt`, updates `updatedAt` |

> [!WARNING]
> This endpoint does not physically delete the user row. It marks the account as deleted.

## `GET /api/me/profile`

Returns the current user's profile.

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
  "avatarUrl": "https://example.com/avatar.png",
  "coverImageUrl": "https://example.com/cover.png",
  "careerGoal": "Backend Developer",
  "currentRole": "Student",
  "publicEmail": "contact@example.com",
  "githubUrl": "https://github.com/example",
  "linkedinUrl": "https://linkedin.com/in/example",
  "personalWebsiteUrl": "https://example.com",
  "isPublic": true
}
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `displayName` | `string/null` | Public display name |
| `headline` | `string/null` | Short headline |
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
| `isPublic` | `boolean` | Controls public portfolio visibility |

## `PATCH /api/me/profile`

Updates current user's profile.

Only provided fields are updated. Blank string normalization is handled by the user service.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `displayName` | `string/null` | No | Max 50 chars |
| `headline` | `string/null` | No | Max 150 chars |
| `bio` | `string/null` | No | Max 500 chars |
| `location` | `string/null` | No | Optional |
| `avatarUrl` | `string/null` | No | Valid URL when provided |
| `coverImageUrl` | `string/null` | No | Valid URL when provided |
| `careerGoal` | `string/null` | No | Optional |
| `currentRole` | `string/null` | No | Optional |
| `publicEmail` | `string/null` | No | Valid email when provided |
| `githubUrl` | `string/null` | No | Valid URL when provided |
| `linkedinUrl` | `string/null` | No | Valid URL when provided |
| `personalWebsiteUrl` | `string/null` | No | Valid URL when provided |
| `isPublic` | `boolean/null` | No | Controls public portfolio visibility |

Example:

```json
{
  "displayName": "Khoa Minh",
  "headline": "Backend Developer",
  "isPublic": true
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
  "headline": "Backend Developer",
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
  "isPublic": true
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Validation or invalid request error |
| Profile not found | Not found |
| Field omitted | Existing value is kept |
| URL field invalid | Validation error |
| Email field invalid | Validation error |
| `isPublic` provided | Updates public portfolio visibility |
| Success | Updates profile fields and `updatedAt` |

## Implementation Notes

| Topic | Notes |
|---|---|
| Portfolio visibility | Public portfolio access depends on `isPublic = true` |
| Account delete | Soft delete updates account status only |
| Current user response | Includes `roles` in `AuthenticatedUserDto` |

## Summary

Use `/api/me` for account-level data and `/api/me/profile` for public profile data. Profile visibility directly controls whether `/api/portfolios/{username}` can return the user's portfolio.
