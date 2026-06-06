# Auth Endpoint Specification

Base route:

```text
/api/auth
```

Source controller: `AuthController`

## Endpoint Summary

| Method | Endpoint | Auth Required | CAPTCHA | Purpose |
|---|---|---:|---:|---|
| `POST` | `/api/auth/register` | No | Yes | Register a local account |
| `POST` | `/api/auth/login` | No | Yes | Login with local email/password |
| `POST` | `/api/auth/registration/verify-email` | No | No | Verify registration email and log in |
| `POST` | `/api/auth/registration/resend-verification` | No | Yes | Resend registration verification code |
| `GET` | `/api/auth/google/login` | No | No | Start Google OAuth login |
| `GET` | `/api/auth/google/callback` | No | No | Handle Google OAuth callback |
| `GET` | `/api/auth/github/login` | No | No | Start GitHub OAuth login |
| `GET` | `/api/auth/github/callback` | No | No | Handle GitHub OAuth callback |
| `POST` | `/api/auth/logout` | No | No | Clear auth cookie |

> [!NOTE]
> The route should be explicitly configured as `[Route("api/auth")]` to avoid documenting `/api/Auth` from `[Route("api/[controller]")]`.

---

## `POST /api/auth/register`

Registers a new local account.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `username` | `string` | Yes | 3-40 chars; letters, numbers, `.`, `_`, `-` only |
| `email` | `string` | Yes | Valid email format |
| `password` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, special char |
| `captchaToken` | `string` | Yes when CAPTCHA is enabled | Cloudflare Turnstile token for action `register` |

### Success Response

Status:

```text
200 OK
```

Body:

```json
{
  "message": "Registration successful. Please verify your email.",
  "email": "user@example.com",
  "requiresEmailVerification": true
}
```

### Response Fields

| Field | Type |
|---|---|
| `message` | `string` |
| `email` | `string` |
| `requiresEmailVerification` | `boolean` |

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Error |
| Missing username/email/password | Validation error |
| Duplicate username | Conflict |
| Duplicate local email | Conflict |
| New account profile | Created automatically with `isPublic = false` |
| Email verification | Verification code is sent after registration |
| CAPTCHA | Required when `Captcha:Enabled = true` |

> [!IMPORTANT]
> Register does not immediately authenticate the user. The user must verify the email first.

---

## `POST /api/auth/login`

Logs in with local email/password.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `emailOrUsername` | `string` | Yes | Email address or username |
| `password` | `string` | Yes | Required |
| `captchaToken` | `string` | Yes when CAPTCHA is enabled | Cloudflare Turnstile token for action `login` |

### Success Response

Status:

```text
200 OK
```

The response sets an HTTP-only cookie:

```text
access_token=<jwt>
```

Body:

```json
{
  "user": {
    "userId": "guid",
    "username": "khoa",
    "email": "user@example.com",
    "status": "active"
  },
  "message": "Logged in successfully"
}
```

> [!CAUTION]
> `LoginResponseDto` contains `accessToken` and `tokenType`, but the controller does not return that DTO directly. It sets `access_token` as an HTTP-only cookie and returns only `user` plus `message`.

### Rules

| Rule | Behavior |
|---|---|
| Invalid email/password | Unauthorized |
| Email not verified | Email-not-verified error |
| Deleted account | Unauthorized |
| Suspended account | Forbidden |
| Successful login | JWT is stored in `access_token` cookie |
| CAPTCHA | Required when `Captcha:Enabled = true` |

---

## `POST /api/auth/registration/verify-email`

Verifies registration email using OTP, then logs the user in.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `otp` | `string` | Yes | Required |

### Success Response

Status:

```text
200 OK
```

Sets:

```text
access_token=<jwt>
```

Body:

```json
{
  "user": {
    "userId": "guid",
    "username": "khoa",
    "email": "user@example.com",
    "status": "active"
  },
  "message": "Email verified successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Email not found | Not found |
| Already verified | Error |
| Missing OTP | Validation error |
| Expired OTP | Error |
| Invalid OTP | Error and attempt count increases |
| Too many attempts | Error |
| Valid OTP | Marks local email as verified and logs user in |

> [!NOTE]
> This endpoint does not require CAPTCHA. The OTP itself is the proof step. The
> resend endpoint is protected instead because it can be abused to send emails.

---

## `POST /api/auth/registration/resend-verification`

Resends registration verification code.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `captchaToken` | `string` | Yes when CAPTCHA is enabled | Cloudflare Turnstile token for action `resend-registration-verification` |

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
| Email already verified | Error |
| Resend too soon | Error with remaining seconds |
| Existing unused tokens | Invalidated before new token is created |
| CAPTCHA | Required when `Captcha:Enabled = true` |

---

## `GET /api/auth/google/login`

Starts Google OAuth login and redirects the user to Google authentication.

| Item | Value |
|---|---|
| Auth scheme | `GoogleDefaults.AuthenticationScheme` |
| Callback | `/api/auth/google/callback` |

---

## `GET /api/auth/google/callback`

Handles Google OAuth callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External authentication succeeds | Login or create account |
| Google email exists on another account | Redirects to login with OAuth error |
| Login succeeds | Sets `access_token` cookie |
| Final redirect | `http://localhost:5173/dashboard` |

### Failure Behavior

| Failure | Response |
|---|---|
| External auth failed | `401 Unauthorized` |
| Service exception | Redirects to `/login?oauthError=<message>` |

> [!NOTE]
> Google accounts are treated as email-verified if Google provides an email.

---

## `GET /api/auth/github/login`

Starts GitHub OAuth login and redirects the user to GitHub authentication.

| Item | Value |
|---|---|
| Auth scheme | `GitHubAuthenticationDefaults.AuthenticationScheme` |
| Callback | `/api/auth/github/callback` |

---

## `GET /api/auth/github/callback`

Handles GitHub OAuth callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External authentication succeeds | Login or create account |
| GitHub account exists | Existing user is logged in |
| New GitHub account | New user/profile/provider rows are created |
| Login succeeds | Sets `access_token` cookie |
| Final redirect | `http://localhost:5173/dashboard` |

### Failure Behavior

| Failure | Response |
|---|---|
| External auth failed | `401 Unauthorized` |
| Missing GitHub ID | Redirects with OAuth error |
| Missing GitHub username | Redirects with OAuth error |
| Email collision | Redirects with OAuth error |

> [!NOTE]
> If the GitHub username is available, the profile `githubUrl` is initialized as `https://github.com/{username}`.

---

## `POST /api/auth/logout`

Clears the auth cookie.

### Success Response

```json
{
  "message": "Logged out successfully"
}
```

### Cookie Behavior

Deletes:

```text
access_token
```

Cookie options used:

| Option | Value |
|---|---|
| `HttpOnly` | `true` |
| `Secure` | `true` |
| `SameSite` | `None` |
