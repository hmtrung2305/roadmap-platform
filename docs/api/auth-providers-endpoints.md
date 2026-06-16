# Auth Providers Endpoint Specification

Base route:

```text
/api/me/auth-providers
```

Source controller:

```text
MeAuthProviderController
```

Related services / DTOs:

```text
IAuthProviderService
IEmailVerificationService
LoginMethodStatusDto
LinkLocalLoginRequestDto
LinkLocalLoginResponseDto
UpdateLocalEmailRequestDto
UpdateLocalEmailResponseDto
VerifyOtpRequestDto
ChangePasswordRequestDto
```

## Summary

Auth provider endpoints manage login methods for the current account.

Users can add local password login, verify local email, change local email, change password, link OAuth providers, and unlink providers.

A user cannot unlink their only usable login method.

## Endpoint Summary

| Method | Endpoint | Auth Required | Purpose |
|---|---|---:|---|
| `GET` | `/api/me/auth-providers` | Yes | Get linked login methods |
| `POST` | `/api/me/auth-providers/local` | Yes | Add local email/password login |
| `POST` | `/api/me/auth-providers/local/resend-verification` | Yes | Resend local login verification OTP |
| `POST` | `/api/me/auth-providers/local/verify` | Yes | Verify linked local login email |
| `POST` | `/api/me/auth-providers/local/email/change-request` | Yes | Request local email change |
| `POST` | `/api/me/auth-providers/local/email/resend-verification` | Yes | Resend local email change OTP |
| `POST` | `/api/me/auth-providers/local/email/verify` | Yes | Verify local email change |
| `PUT` | `/api/me/auth-providers/local/password` | Yes | Change local password |
| `GET` | `/api/me/auth-providers/github/link` | Yes | Start GitHub account linking |
| `GET` | `/api/me/auth-providers/github/callback` | No | Handle GitHub link callback |
| `GET` | `/api/me/auth-providers/google/link` | Yes | Start Google account linking |
| `GET` | `/api/me/auth-providers/google/callback` | No | Handle Google link callback |
| `DELETE` | `/api/me/auth-providers/{provider}` | Yes | Unlink login method |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | Yes for all non-callback endpoints |
| Auth type | Bearer token through authenticated `access_token` cookie |
| User id source | `ClaimTypes.NameIdentifier` through `User.GetUserId()` |
| OAuth callback auth | No active user auth required; uses external OAuth properties with `linking_user_id` |

> [!IMPORTANT]
> OAuth link callbacks are anonymous by route because the linking user id is carried in OAuth properties. They still fail if the linking session is missing or invalid.

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Error format | Shared `ApiErrorResponse` object |
| Rate limit policy | `AuthStrict` for mutation and OAuth-link endpoints |
| Default account settings redirect | `${Frontend:BaseUrl}/settings/account` |

## `GET /api/me/auth-providers`

Returns login method status for local, Google, and GitHub.

### Success Response

Status:

```text
200 OK
```

Body:

```json
[
  {
    "provider": "local",
    "displayName": "Local",
    "isLinked": false,
    "canUnlink": false,
    "requiresVerification": true
  },
  {
    "provider": "google",
    "displayName": "Google",
    "isLinked": true,
    "canUnlink": true,
    "requiresVerification": false
  },
  {
    "provider": "github",
    "displayName": "GitHub",
    "isLinked": false,
    "canUnlink": false,
    "requiresVerification": false
  }
]
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `provider` | `string` | `local`, `google`, or `github` |
| `displayName` | `string` | UI display name |
| `isLinked` | `boolean` | For local, true only when verified |
| `canUnlink` | `boolean` | True only when the provider is linked and more than one usable provider exists |
| `requiresVerification` | `boolean` | True when local login exists but email is not verified |

### Rules

| Rule | Behavior |
|---|---|
| User not found | Not found |
| Pending local login | `local.isLinked = false`, `local.requiresVerification = true` |
| Verified local login | `local.isLinked = true` |
| OAuth providers | `requiresVerification = false` |

## `POST /api/me/auth-providers/local`

Adds local email/password login to an existing OAuth account and sends verification OTP.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `password` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, and special char |

Example:

```json
{
  "email": "user@example.com",
  "password": "Password@123"
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
  "message": "Password login is pending verification. Please verify your email to finish linking it.",
  "email": "user@example.com",
  "requiresEmailVerification": true,
  "verificationPurpose": "link_local",
  "canResendVerification": true
}
```

### Rules

| Rule | Behavior |
|---|---|
| Missing body | Invalid request |
| User not found | Not found |
| Account already has verified local login | Conflict |
| Account already has pending local login | Returns pending verification response |
| Email already registered by another local provider | Conflict |
| Success | Creates pending local provider and sends OTP |

## `POST /api/me/auth-providers/local/resend-verification`

Resends verification OTP for a pending linked local login.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "message": "Verification code sent"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login missing | Not found |
| Local email missing | Invalid request |
| Local email already verified | Invalid request |
| Resend cooldown active | Invalid request |
| Success | Sends a new `link_local` OTP |

## `POST /api/me/auth-providers/local/verify`

Verifies linked local login email.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `otp` | `string` | Yes | Required |

Example:

```json
{
  "otp": "123456"
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
  "message": "Local login email verified successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login missing | Not found |
| Local email missing | Invalid request |
| Local email already verified | Invalid request |
| OTP invalid or expired | Invalid request |
| Success | Sets `emailVerifiedAt` on local provider |

## `POST /api/me/auth-providers/local/email/change-request`

Starts a local email change and sends OTP to the new email.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `newEmail` | `string` | Yes | Valid email format |

Example:

```json
{
  "newEmail": "new@example.com"
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
  "message": "Verification code sent",
  "email": "new@example.com",
  "requiresEmailVerification": true,
  "verificationPurpose": "change_email",
  "canResendVerification": true
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login missing | Not found |
| Account has no local password login | Invalid request |
| New email same as current email | Invalid request |
| New email already registered | Conflict |
| Success | Saves `pendingEmail` and sends `change_email` OTP |

## `POST /api/me/auth-providers/local/email/resend-verification`

Resends OTP for a pending local email change.

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "message": "Verification code sent"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Local login missing | Not found |
| No pending email change | Invalid request |
| Resend cooldown active | Invalid request |
| Success | Sends a new `change_email` OTP to `pendingEmail` |

## `POST /api/me/auth-providers/local/email/verify`

Verifies and applies a pending local email change.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `otp` | `string` | Yes | Required |

Example:

```json
{
  "otp": "123456"
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
  "message": "Email changed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| No pending email change | Invalid request |
| Pending email already registered | Conflict |
| OTP invalid or expired | Invalid request |
| Success | Updates `email`, `providerUserId`, clears `pendingEmail`, and sets `emailVerifiedAt` |

## `PUT /api/me/auth-providers/local/password`

Changes local password.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `currentPassword` | `string` | Yes | Required |
| `newPassword` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, and special char |

Example:

```json
{
  "currentPassword": "Password@123",
  "newPassword": "NewPassword@123"
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
  "message": "Password changed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Account has no local password login | Invalid request |
| Local email not verified | Invalid request |
| Current password incorrect | Unauthorized |
| New password same as current | Invalid request |
| Success | Replaces password hash |

## `GET /api/me/auth-providers/github/link`

Starts GitHub account linking for the current authenticated user.

### Query Parameters

| Parameter | Type | Required | Default | Notes |
|---|---|---:|---|---|
| `returnUrl` | `string` | No | `/settings/account` | Frontend path to redirect to after successful link |

### Success Behavior

| Step | Behavior |
|---|---|
| Current user id | Stored in OAuth properties as `linking_user_id` |
| Return URL | Stored as `frontend_return_url` after safety normalization |
| Unsafe return URL | Falls back to `/settings/account` |
| OAuth scheme | `GitHubAuthenticationDefaults.AuthenticationScheme` |

> [!IMPORTANT]
> `returnUrl` must be a relative frontend path. Values starting with `//`, containing `://`, or containing backslashes are rejected and replaced with `/settings/account`.

## `GET /api/me/auth-providers/github/callback`

Handles GitHub account linking callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External auth succeeds | Reads `linking_user_id` from OAuth properties |
| GitHub access token | Passed to provider service when available |
| Link succeeds | Adds GitHub provider row |
| Final redirect | `${Frontend:BaseUrl}<returnUrl>` or `/settings/account` |

### Failure Behavior

| Failure | Behavior |
|---|---|
| External auth failed | `401 Unauthorized` with `OAUTH_AUTHENTICATION_FAILED` |
| Missing linking session | `401 Unauthorized` with `OAUTH_LINKING_SESSION_INVALID` |
| GitHub already linked to current user | Conflict |
| GitHub linked to another user | Conflict |

## `GET /api/me/auth-providers/google/link`

Starts Google account linking for the current authenticated user.

### Success Behavior

| Step | Behavior |
|---|---|
| Current user id | Stored in OAuth properties as `linking_user_id` |
| OAuth scheme | `GoogleDefaults.AuthenticationScheme` |
| Return URL | No query parameter support in current controller |

## `GET /api/me/auth-providers/google/callback`

Handles Google account linking callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External auth succeeds | Reads `linking_user_id` from OAuth properties |
| Link succeeds | Adds Google provider row |
| Final redirect | `${Frontend:BaseUrl}/settings/account` |

### Failure Behavior

| Failure | Behavior |
|---|---|
| External auth failed | `401 Unauthorized` with `OAUTH_AUTHENTICATION_FAILED` |
| Missing linking session | `401 Unauthorized` with `OAUTH_LINKING_SESSION_INVALID` |
| Google already linked to current user | Conflict |
| Google linked to another user | Conflict |

## `DELETE /api/me/auth-providers/{provider}`

Unlinks a login method.

### Path Parameters

| Parameter | Type | Required | Notes |
|---|---|---:|---|
| `provider` | `string` | Yes | `local`, `google`, or `github` |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "message": "github login removed successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Unsupported provider | Invalid request |
| Provider not linked | Not found |
| Provider is only usable login method | Invalid request or conflict |
| Success | Removes provider row |

## Implementation Notes

| Topic | Notes |
|---|---|
| Pending local login | Local provider exists but is not usable until `emailVerifiedAt` is set |
| Linking session | OAuth properties carry `linking_user_id` |
| Safe redirects | GitHub link return path is normalized to prevent external redirects |
| Account settings redirect | OAuth linking returns to `/settings/account` by default |

## Summary

These endpoints manage login methods for an existing account. The main backend changes are the new local email-change resend endpoint, accurate local pending verification status, and account-link redirects returning to settings instead of dashboard.
