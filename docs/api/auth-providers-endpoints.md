# Auth Providers Endpoint Specification

Base route:

```text
/api/me/auth-providers
```

Source controller: `MeAuthProviderController`

All non-callback endpoints require authentication.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/me/auth-providers` | Yes | Get linked login methods |
| `POST` | `/api/me/auth-providers/local` | Yes | Add local email/password login |
| `POST` | `/api/me/auth-providers/local/resend-verification` | Yes | Resend local login verification |
| `POST` | `/api/me/auth-providers/local/verify` | Yes | Verify linked local login email |
| `POST` | `/api/me/auth-providers/local/email/change-request` | Yes | Request local email change |
| `POST` | `/api/me/auth-providers/local/email/verify` | Yes | Verify local email change |
| `PUT` | `/api/me/auth-providers/local/password` | Yes | Change local password |
| `GET` | `/api/me/auth-providers/github/link` | Yes | Start GitHub account linking |
| `GET` | `/api/me/auth-providers/github/callback` | No | Handle GitHub link callback |
| `GET` | `/api/me/auth-providers/google/link` | Yes | Start Google account linking |
| `GET` | `/api/me/auth-providers/google/callback` | No | Handle Google link callback |
| `DELETE` | `/api/me/auth-providers/{provider}` | Yes | Unlink login method |

> [!IMPORTANT]
> A user cannot unlink their only login method.

---

## `GET /api/me/auth-providers`

Returns login method status for local, Google, and GitHub.

### Success Response

```json
[
  {
    "provider": "local",
    "displayName": "Local",
    "isLinked": true,
    "canUnlink": false,
    "requiresVerification": false
  },
  {
    "provider": "google",
    "displayName": "Google",
    "isLinked": false,
    "canUnlink": false,
    "requiresVerification": false
  },
  {
    "provider": "github",
    "displayName": "GitHub",
    "isLinked": true,
    "canUnlink": true,
    "requiresVerification": false
  }
]
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `provider` | `string` | `local`, `google`, or `github` |
| `displayName` | `string` | UI display name |
| `isLinked` | `boolean` | Whether the login method exists |
| `canUnlink` | `boolean` | True only when linked and user has more than one provider |
| `requiresVerification` | `boolean` | Present in DTO; indicates whether provider needs verification if service populates it |

> [!CAUTION]
> The DTO includes `requiresVerification`, but the uploaded service does not assign it. In current behavior it will default to `false` unless the service is updated.

---

## `POST /api/me/auth-providers/local`

Adds local email/password login to an existing OAuth account.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `password` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, special char |

### Success Response

```json
{
  "message": "Password login added. Verification code sent to email."
}
```

### Rules

| Rule | Behavior |
|---|---|
| User already has verified local login | Conflict |
| User already has pending local verification | Conflict |
| Email already registered by another local account | Conflict |
| Success | Creates local provider and sends OTP |

> [!NOTE]
> The local login is not fully usable until the email is verified.

---

## `POST /api/me/auth-providers/local/resend-verification`

Resends verification code for a linked local login.

### Success Response

```json
{
  "message": "Verification code sent"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login not found | Not found |
| Local email missing | Error |
| Local email already verified | Error |
| Resend cooldown active | Error |

---

## `POST /api/me/auth-providers/local/verify`

Verifies linked local login email.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `otp` | `string` | Yes | Required |

### Success Response

```json
{
  "message": "Local login email verified successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login not found | Not found |
| Email already verified | Error |
| Invalid OTP | Error |
| Valid OTP | Sets `emailVerifiedAt` |

---

## `POST /api/me/auth-providers/local/email/change-request`

Requests local email change.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `newEmail` | `string` | Yes | Valid email format |

### Success Response

```json
{
  "message": "Verification code sent"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login not found | Not found |
| Account has no local password login | Error |
| New email same as current email | Error |
| New email already registered | Conflict |
| Success | Saves `pendingEmail` and sends OTP |

> [!IMPORTANT]
> This endpoint only starts the email change. It does not immediately replace the account email.

---

## `POST /api/me/auth-providers/local/email/verify`

Verifies and applies local email change.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `otp` | `string` | Yes | Required |

### Success Response

```json
{
  "message": "Email changed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| No pending email change | Error |
| Pending email already registered | Conflict |
| Invalid OTP | Error |
| Valid OTP | Updates `email`, `providerUserId`, clears `pendingEmail`, sets `emailVerifiedAt` |

---

## `PUT /api/me/auth-providers/local/password`

Changes local password.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `currentPassword` | `string` | Yes | Required |
| `newPassword` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, special char |

### Success Response

```json
{
  "message": "Password changed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Account has no local password login | Error |
| Local email not verified | Error |
| Current password incorrect | Unauthorized |
| New password same as current | Error |
| Success | Replaces password hash |

---

## `GET /api/me/auth-providers/github/link`

Starts GitHub linking for the current authenticated user.

### Behavior

Stores current user id in OAuth properties using:

```text
linking_user_id
```

Then redirects to GitHub OAuth.

---

## `GET /api/me/auth-providers/github/callback`

Handles GitHub linking callback.

### Behavior

| Condition | Behavior |
|---|---|
| GitHub auth failed | Unauthorized |
| Missing linking session | Unauthorized |
| GitHub already linked to current user | Conflict |
| GitHub linked to another user | Conflict |
| Success | Adds GitHub provider and redirects to `/dashboard` |

---

## `GET /api/me/auth-providers/google/link`

Starts Google linking for the current authenticated user.

### Behavior

Stores current user id in OAuth properties using:

```text
linking_user_id
```

Then redirects to Google OAuth.

---

## `GET /api/me/auth-providers/google/callback`

Handles Google linking callback.

### Behavior

| Condition | Behavior |
|---|---|
| Google auth failed | Unauthorized |
| Missing linking session | Unauthorized |
| Google already linked to current user | Conflict |
| Google linked to another user | Conflict |
| Success | Adds Google provider and redirects to `/dashboard` |

---

## `DELETE /api/me/auth-providers/{provider}`

Unlinks a login method.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `provider` | `string` | Yes | `local`, `google`, or `github` |

### Success Response

```json
{
  "message": "github login removed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Unsupported provider | Error |
| Provider not linked | Not found |
| Provider is only login method | Error |
| Success | Provider row is removed |
