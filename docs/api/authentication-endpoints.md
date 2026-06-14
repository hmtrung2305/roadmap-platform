# Auth Endpoint Specification

Base route:

```text
/api/auth
```

Source controller: `AuthController`

## Endpoint Summary

| Method | Endpoint | Auth Required | CAPTCHA | Purpose |
|---|---|---:|---:|---|
| `POST` | `/api/auth/register` | No | Yes | Start local registration and create a pending registration |
| `POST` | `/api/auth/login` | No | Yes | Login with local email/username and password |
| `POST` | `/api/auth/registration/verify-email` | No | No | Verify pending local registration email, create the user, and log in |
| `POST` | `/api/auth/registration/resend-verification` | No | Yes | Resend pending registration verification code |
| `GET` | `/api/auth/google/login` | No | No | Start Google OAuth login |
| `GET` | `/api/auth/google/callback` | No | No | Handle Google OAuth callback |
| `GET` | `/api/auth/github/login` | No | No | Start GitHub OAuth login |
| `GET` | `/api/auth/github/callback` | No | No | Handle GitHub OAuth callback |
| `POST` | `/api/auth/logout` | No | No | Clear auth cookie |

> [!NOTE]
> The route should be explicitly configured as `[Route("api/auth")]` to avoid documenting `/api/Auth` from `[Route("api/[controller]")]`.

## Local Registration Model

Local registration now uses `pending_local_registration`.

The real `user`, `user_profile`, `user_auth_provider`, and `user_role` rows are created only after the registration OTP is verified.

| Stage | Storage |
|---|---|
| Register submitted | `pending_local_registration` |
| OTP token created | `email_verification_token.pending_local_registration_id` |
| OTP verified | Real user/profile/provider/role rows are created |
| Registration completed | `pending_local_registration.used_at` is set |

> [!IMPORTANT]
> Pending registrations do not reserve usernames permanently. Username uniqueness is enforced only by `public.user.username_normalized` when the user is actually created.

---

## `POST /api/auth/register`

Starts a new local registration.

This endpoint does not create a real user immediately. It creates or updates a pending local registration and sends an OTP.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `username` | `string` | Yes | 3-40 chars; letters, numbers, `.`, `_`, `-` only |
| `email` | `string` | Yes | Valid email format; common email domain typo guard |
| `password` | `string` | Yes | Min 8 chars; uppercase, lowercase, number, special char |
| `captchaToken` | `string` | Yes when CAPTCHA is enabled | Cloudflare Turnstile token for action `register` |

### Success Response

Status:

```text
202 Accepted
```

Body:

```json
{
  "message": "Registration started. Please verify your email.",
  "email": "user@example.com",
  "requiresEmailVerification": true,
  "verificationPurpose": "register",
  "canResendVerification": true
}
```

### Pending Registration Response

If the same pending email already exists and has not expired, the endpoint returns the verification flow response instead of creating another pending record.

Status:

```text
202 Accepted
```

Body:

```json
{
  "message": "This email already has a pending registration. Verify your email to continue with the original account details.",
  "email": "user@example.com",
  "requiresEmailVerification": true,
  "verificationPurpose": "register",
  "canResendVerification": true
}
```

### Response Fields

| Field | Type | Description |
|---|---|---|
| `message` | `string` | User-facing next-step message |
| `email` | `string` | Email that needs verification |
| `requiresEmailVerification` | `boolean` | Always `true` for successful registration start |
| `verificationPurpose` | `string` | `register` |
| `canResendVerification` | `boolean` | Whether the frontend should show resend support |

### Rules

| Rule | Behavior |
|---|---|
| Missing request body | Error |
| Missing username/email/password | Validation error |
| Invalid email format | Validation error |
| Common email domain typo | Validation error with suggestion |
| Duplicate verified local email | Conflict |
| Duplicate real username in `public.user` | Conflict |
| Duplicate pending email | Returns pending verification response |
| Duplicate pending username | Allowed |
| Pending registration expired | Pending row may be refreshed with new submitted details |
| New account profile | Not created until OTP verification succeeds |
| Local auth provider | Not created until OTP verification succeeds |
| Learner role | Not assigned until OTP verification succeeds |
| Email verification | OTP code is sent after pending registration is saved |
| CAPTCHA | Required when `Captcha:Enabled = true` |

> [!IMPORTANT]
> Register does not authenticate the user. The user must verify the email first.

---

## `POST /api/auth/login`

Logs in with local email/username and password.

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
| Pending registration only | Login fails because no real user exists yet |
| Legacy local provider with unverified email | Email-not-verified error |
| Deleted account | Unauthorized |
| Suspended account | Forbidden |
| Successful login | JWT is stored in `access_token` cookie |
| CAPTCHA | Required when `Captcha:Enabled = true` |

> [!NOTE]
> A pending local registration is not a loginable account. The user must complete `/api/auth/registration/verify-email` first.

---

## `POST /api/auth/registration/verify-email`

Verifies registration email using OTP, creates the real user, then logs the user in.

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
| Pending registration not found | Not found |
| Pending registration expired | Error |
| Missing OTP | Validation error |
| Expired OTP | Error |
| Invalid OTP | Error and attempt count increases |
| Too many attempts | Error |
| Username taken before verification completed | Conflict; user must restart registration with another username |
| Email registered before verification completed | Conflict |
| Valid OTP | Creates `user`, `user_profile`, local `user_auth_provider`, and learner `user_role` |
| Completed pending registration | Sets `pending_local_registration.used_at` |
| Successful verification | Logs user in by setting `access_token` cookie |

> [!NOTE]
> This endpoint does not require CAPTCHA. The OTP itself is the proof step. The resend endpoint is protected instead because it can be abused to send emails.

---

## `POST /api/auth/registration/resend-verification`

Resends the pending registration verification code.

### Request Body

| Field | Type | Required | Validation |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `captchaToken` | `string` | Yes when CAPTCHA is enabled | Cloudflare Turnstile token for action `resend-registration-verification` |

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
| Pending registration not found | Not found |
| Pending registration already used | Not found or error |
| Pending registration expired | Error |
| Resend too soon | Error with remaining seconds |
| Existing unused tokens | Invalidated before new token is created |
| CAPTCHA | Required when `Captcha:Enabled = true` |

---

## Email Verification Token Ownership

Registration OTP tokens are tied to pending registrations, not real users.

| Purpose | Token owner |
|---|---|
| `register` | `pending_local_registration_id` |
| `link_local` | `user_id` |
| `change_email` | `user_id` |

Rules:

- A token must have exactly one owner.
- Registration tokens use `pending_local_registration_id`.
- Authenticated account-management tokens use `user_id`.

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

Status:

```text
200 OK
```

Body:

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
