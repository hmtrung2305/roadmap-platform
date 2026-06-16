# Authentication Endpoint Specification

Base route:

```text
/api/auth
```

Source controller:

```text
AuthController
```

Related services / DTOs:

```text
IAuthService
RegisterRequestDto
LoginRequestDto
VerifyRegistrationEmailRequestDto
ResendRegistrationVerificationRequestDto
RegistrationResponseDto
LoginResponseDto
EmailVerificationRequiredResponseDto
```

## Summary

Authentication supports local registration with OTP verification, local login, OAuth login, and logout.

Local registration creates a pending registration first. The real user, profile, auth provider, and default learner role are created only after email OTP verification succeeds.

OAuth login stores the JWT in the `access_token` HTTP-only cookie and redirects the frontend to `/roadmaps`.

## Endpoint Summary

| Method | Endpoint | Auth Required | CAPTCHA | Purpose |
|---|---|---:|---:|---|
| `POST` | `/api/auth/register` | No | Yes | Start local registration and send OTP |
| `POST` | `/api/auth/login` | No | Yes | Login with email/username and password |
| `POST` | `/api/auth/registration/verify-email` | No | No | Verify pending registration and create the real account |
| `POST` | `/api/auth/registration/resend-verification` | No | Yes | Resend pending registration OTP |
| `GET` | `/api/auth/google/login` | No | No | Start Google OAuth login |
| `GET` | `/api/auth/google/callback` | No | No | Handle Google OAuth callback |
| `GET` | `/api/auth/github/login` | No | No | Start GitHub OAuth login |
| `GET` | `/api/auth/github/callback` | No | No | Handle GitHub OAuth callback |
| `POST` | `/api/auth/logout` | No | No | Clear auth cookie |

## Authentication

| Requirement | Details |
|---|---|
| Auth required | No for all endpoints in this file |
| Auth type | HTTP-only cookie after successful login |
| Token source | `access_token` cookie |
| CAPTCHA | `RequireCaptchaAttribute` on register, login, and registration resend |

> [!IMPORTANT]
> Login and successful registration verification do not return the JWT in the response body. They set `access_token` as an HTTP-only cookie.

## Common Response Notes

| Topic | Details |
|---|---|
| JSON casing | camelCase |
| Date format | ISO 8601 datetime string |
| Error format | Shared `ApiErrorResponse` object |
| OAuth success redirect | `${Frontend:BaseUrl}/roadmaps` |
| OAuth error redirect | `${Frontend:BaseUrl}/login?oauthError=<message>` |

## `POST /api/auth/register`

Starts a local registration by creating or updating a pending registration and sending an OTP.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `username` | `string` | Yes | 3-40 chars; letters, numbers, `.`, `_`, `-` only |
| `email` | `string` | Yes | Valid email format; common domain typo guard |
| `password` | `string` | Yes | Min 8 chars; must include uppercase, lowercase, number, and special char |
| `captchaToken` | `string/null` | Required when CAPTCHA is enabled | Turnstile token for action `register` |

Example:

```json
{
  "username": "khoa",
  "email": "user@example.com",
  "password": "Password@123",
  "captchaToken": "turnstile-token"
}
```

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

### Rules

| Rule | Behavior |
|---|---|
| Real user creation | Deferred until OTP verification succeeds |
| Pending registration | Stored in `pending_local_registration` |
| Duplicate verified local email | Conflict |
| Duplicate real username | Conflict |
| Duplicate pending email | Returns pending verification response |
| Duplicate pending username | Allowed |
| Expired pending registration | Can be refreshed with new submitted details |
| OTP send | Creates `email_verification_token` for purpose `register` |
| CAPTCHA enabled | Request must include a valid CAPTCHA token |

## `POST /api/auth/login`

Logs in with local email/username and password.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `emailOrUsername` | `string` | Yes | Email address or username |
| `password` | `string` | Yes | Required |
| `captchaToken` | `string/null` | Required when CAPTCHA is enabled | Turnstile token for action `login` |

Example:

```json
{
  "emailOrUsername": "user@example.com",
  "password": "Password@123",
  "captchaToken": "turnstile-token"
}
```

### Success Response

Status:

```text
200 OK
```

Cookie:

```text
access_token=<jwt>
```

Body:

```json
{
  "user": {
    "userId": "11111111-1111-1111-1111-111111111111",
    "username": "khoa",
    "email": "user@example.com",
    "status": "active",
    "roles": ["learner"]
  },
  "message": "Logged in successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Invalid credentials | Unauthorized |
| Pending registration only | Cannot log in because no real user exists yet |
| Local email unverified | Returns email verification error details |
| Suspended account | Forbidden |
| Deleted account | Unauthorized |
| Successful login | Sets `access_token` cookie |

## `POST /api/auth/registration/verify-email`

Verifies a pending registration OTP, creates the real account, and logs the user in.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `otp` | `string` | Yes | Required |

Example:

```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

### Success Response

Status:

```text
200 OK
```

Cookie:

```text
access_token=<jwt>
```

Body:

```json
{
  "user": {
    "userId": "11111111-1111-1111-1111-111111111111",
    "username": "khoa",
    "email": "user@example.com",
    "status": "active",
    "roles": ["learner"]
  },
  "message": "Email verified successfully"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Pending registration missing | Not found |
| Pending registration expired | Invalid request |
| OTP expired | Invalid request |
| OTP invalid | Invalid request and attempt count increases |
| Too many attempts | Invalid request |
| Username taken before verification | Conflict |
| Email registered before verification | Conflict |
| Successful verification | Creates `user`, `user_profile`, local `user_auth_provider`, and learner `user_role` |
| Completed pending registration | Sets `pending_local_registration.used_at` |
| Successful login | Sets `access_token` cookie |

## `POST /api/auth/registration/resend-verification`

Resends the OTP for an unused pending registration.

### Request Body

| Field | Type | Required | Validation / Notes |
|---|---|---:|---|
| `email` | `string` | Yes | Valid email format |
| `captchaToken` | `string/null` | Required when CAPTCHA is enabled | Turnstile token for action `resend-registration-verification` |

Example:

```json
{
  "email": "user@example.com",
  "captchaToken": "turnstile-token"
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
  "message": "Verification code sent"
}
```

### Rules

| Rule | Behavior |
|---|---|
| Pending registration missing | Not found |
| Pending registration already used | Not found or invalid request |
| Pending registration expired | Invalid request |
| Resend cooldown active | Invalid request |
| Existing unused tokens | Invalidated before a new token is created |
| CAPTCHA enabled | Request must include a valid CAPTCHA token |

## `GET /api/auth/google/login`

Starts Google OAuth login.

### Success Behavior

| Step | Behavior |
|---|---|
| Controller action | Returns an OAuth challenge |
| OAuth scheme | `GoogleDefaults.AuthenticationScheme` |
| Callback | `/api/auth/google/callback` |

## `GET /api/auth/google/callback`

Handles Google OAuth login callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External auth succeeds | Logs in or creates the account |
| JWT | Stored in `access_token` cookie |
| External cookie | Signed out after login succeeds |
| Final redirect | `${Frontend:BaseUrl}/roadmaps` |

### Failure Behavior

| Failure | Behavior |
|---|---|
| External auth failed | `401 Unauthorized` with `OAUTH_AUTHENTICATION_FAILED` |
| Service exception | Redirects to `/login?oauthError=<message>` |

## `GET /api/auth/github/login`

Starts GitHub OAuth login.

### Success Behavior

| Step | Behavior |
|---|---|
| Controller action | Returns an OAuth challenge |
| OAuth scheme | `GitHubAuthenticationDefaults.AuthenticationScheme` |
| Callback | `/api/auth/github/callback` |

## `GET /api/auth/github/callback`

Handles GitHub OAuth login callback.

### Success Behavior

| Step | Behavior |
|---|---|
| External auth succeeds | Logs in or creates the account |
| GitHub access token | Passed to auth service for storage when available |
| JWT | Stored in `access_token` cookie |
| External cookie | Signed out after login succeeds |
| Final redirect | `${Frontend:BaseUrl}/roadmaps` |

### Failure Behavior

| Failure | Behavior |
|---|---|
| External auth failed | `401 Unauthorized` with `OAUTH_AUTHENTICATION_FAILED` |
| Missing GitHub id or username | Redirects to `/login?oauthError=<message>` |
| Email collision | Redirects to `/login?oauthError=<message>` |

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

Cookie options used by login and logout:

| Option | Value |
|---|---|
| `HttpOnly` | `true` |
| `Secure` | `true` |
| `SameSite` | `None` |
| `Expires` | 60 minutes from current UTC time |

## Implementation Notes

| Topic | Notes |
|---|---|
| Pending registration | Real account rows are created after OTP verification |
| Email sending | Register and resend send OTP email |
| OAuth redirects | Success redirects to `/roadmaps`; errors redirect to `/login` with `oauthError` |
| Security | JWT is stored only in an HTTP-only cookie |

## Summary

Authentication is cookie-based after login. Local accounts require OTP verification before the user row exists. OAuth success now redirects users to the roadmap selection flow instead of a dashboard page.
