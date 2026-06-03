# Current User and Profile Endpoint Specification

Base route:

```text
/api/me
```

Source controller: `MeController`

All endpoints in this file require authentication.

## Endpoint Summary

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/me` | Get current user account |
| `PATCH` | `/api/me` | Update current username |
| `DELETE` | `/api/me` | Soft-delete current account |
| `GET` | `/api/me/profile` | Get current user's profile |
| `PATCH` | `/api/me/profile` | Update current user's profile |

---

## `GET /api/me`

Returns current authenticated user.

### Success Response

```json
{
  "userId": "guid",
  "username": "khoa",
  "email": "user@example.com",
  "status": "active"
}
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | `guid` | Current user id |
| `username` | `string` | Account username |
| `email` | `string/null` | Preferred email from auth providers |
| `status` | `string` | Example: `active`, `deleted`, `suspended` |

> [!NOTE]
> Email is selected from linked auth providers, prioritizing local login when available.

---

## `PATCH /api/me`

Updates current username.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `username` | `string` | Yes | 3-40 chars; letters, numbers, `.`, `_`, `-` only |

### Success Response

```json
{
  "userId": "guid",
  "username": "new_username",
  "email": "user@example.com",
  "status": "active"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Error |
| Missing username | Validation error |
| Duplicate username | Conflict |
| Success | Updates `username`, `usernameNormalized`, `updatedAt` |

---

## `DELETE /api/me`

Soft-deletes current account.

### Success Response

```text
204 No Content
```

### Rules

| Rule | Behavior |
|---|---|
| User not found | Not found |
| Account already deleted | Conflict |
| Success | Sets status to `deleted`, sets `deletedAt`, updates `updatedAt` |

> [!CAUTION]
> This does not physically delete the user row. It marks the account as deleted.

---

## `GET /api/me/profile`

Returns current user's profile.

### Success Response

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
| `isPublic` | `boolean` |

---

## `PATCH /api/me/profile`

Updates current user's profile.

### Request Body

All fields are optional. Only provided fields are updated.

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `displayName` | `string/null` | No | Max 50 chars; blank becomes `null` |
| `headline` | `string/null` | No | Max 150 chars; blank becomes `null` |
| `bio` | `string/null` | No | Max 500 chars; blank becomes `null` |
| `location` | `string/null` | No | Blank becomes `null` |
| `avatarUrl` | `string/null` | No | Valid URL; blank becomes `null` |
| `coverImageUrl` | `string/null` | No | Valid URL; blank becomes `null` |
| `careerGoal` | `string/null` | No | Blank becomes `null` |
| `currentRole` | `string/null` | No | Blank becomes `null` |
| `publicEmail` | `string/null` | No | Valid email; blank becomes `null` |
| `githubUrl` | `string/null` | No | Valid URL; blank becomes `null` |
| `linkedinUrl` | `string/null` | No | Valid URL; blank becomes `null` |
| `personalWebsiteUrl` | `string/null` | No | Valid URL; blank becomes `null` |
| `isPublic` | `boolean` | No | Controls public portfolio visibility |

### Success Response

Returns the updated profile object.

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Error |
| Profile not found | Not found |
| Field is omitted | Existing value is kept |
| Field is blank string | Stored as `null` |
| `isPublic` provided | Updates public visibility |
| Success | Updates `updatedAt` |

> [!IMPORTANT]
> Public portfolio access depends on `isPublic = true`.
