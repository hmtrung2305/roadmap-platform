# CAPTCHA Protection

This project uses Cloudflare Turnstile for CAPTCHA verification on public,
abuse-prone authentication actions.

## Why CAPTCHA Is Used

CAPTCHA is added only where it protects a real risk:

| Flow | CAPTCHA | Reason |
|---|---:|---|
| Register local account | Yes | Prevent automated account creation |
| Login with password | Yes | Slow down credential stuffing and brute-force attempts |
| Resend registration verification code | Yes | Prevent email-sending abuse |
| Verify registration email OTP | No | OTP is already a proof step; resend is the abuse-prone action |
| Google/GitHub OAuth | No | OAuth provider handles challenge and abuse checks |
| Authenticated user actions | No by default | Prefer authorization, rate limits, or re-authentication |

Future public forms such as forgot password, contact forms, public comments, or
anonymous uploads should be reviewed with the same rule: add CAPTCHA only when
the action can be automated to create accounts, send emails, brute-force secrets,
or consume expensive resources.

## Backend Configuration

Backend settings are under the `Captcha` section.

Default in `RoadmapPlatform.Api/appsettings.json`:

```json
{
  "Captcha": {
    "Enabled": false,
    "Provider": "Turnstile",
    "SiteKey": "",
    "SecretKey": "",
    "VerifyUrl": "https://challenges.cloudflare.com/turnstile/v0/siteverify"
  }
}
```

For local development, store the secret key with ASP.NET Core User Secrets from
the API project:

```powershell
cd src/backend/RoadmapPlatform.Api

dotnet user-secrets set "Captcha:Enabled" "true"
dotnet user-secrets set "Captcha:SecretKey" "your_turnstile_secret_key"
```

Keep `Captcha:SecretKey` only on the backend. Never expose it in frontend code
or commit it to Git.

## Frontend Configuration

Frontend uses Vite environment variables.

Create `src/frontend/.env.local`:

```env
VITE_CAPTCHA_PROVIDER=Turnstile
VITE_CAPTCHA_SITE_KEY=your_turnstile_site_key
```

Restart the Vite dev server after editing `.env.local`.

If `VITE_CAPTCHA_SITE_KEY` is missing, the frontend CAPTCHA widget stays hidden.
This matches the default backend config where `Captcha:Enabled` is `false`.

## Getting Cloudflare Turnstile Keys

1. Open Cloudflare Dashboard.
2. Go to Turnstile.
3. Create a new widget.
4. Add allowed hostnames:
   - `localhost` for local development.
   - The production domain for deployment.
5. Choose a widget mode. `Managed` is the recommended default.
6. Copy the generated keys:
   - Site key -> frontend `VITE_CAPTCHA_SITE_KEY`.
   - Secret key -> backend `Captcha:SecretKey`.

## Backend Implementation

The reusable backend pieces are:

| File | Purpose |
|---|---|
| `RoadmapPlatform.Application/Interfaces/Security/ICaptchaService.cs` | CAPTCHA contracts and protected request marker |
| `RoadmapPlatform.Infrastructure/Configurations/CaptchaSettings.cs` | CAPTCHA configuration binding |
| `RoadmapPlatform.Infrastructure/Services/Security/TurnstileCaptchaService.cs` | Cloudflare Turnstile verification |
| `RoadmapPlatform.Api/Filters/RequireCaptchaAttribute.cs` | Action filter used on protected endpoints |

Protected endpoints use the attribute:

```csharp
[RequireCaptcha("register")]
```

The action value is checked against Turnstile's returned `action`, which helps
avoid reusing a token created for another form.

## Frontend Implementation

The reusable frontend component is:

```text
src/frontend/src/components/common/TurnstileCaptcha.jsx
```

Current pages using it:

| Page | Action |
|---|---|
| `LoginPage.jsx` | `login` |
| `RegisterPage.jsx` | `register` |
| `VerifyEmailPage.jsx` resend button | `resend-registration-verification` |

Tokens are sent in the request body as `captchaToken`/`CaptchaToken` and reset
after each submit because Turnstile tokens are single-use.

## API Contract

The following public endpoints require `captchaToken` when backend CAPTCHA is
enabled:

| Endpoint | Action |
|---|---|
| `POST /api/auth/register` | `register` |
| `POST /api/auth/login` | `login` |
| `POST /api/auth/registration/resend-verification` | `resend-registration-verification` |

If CAPTCHA verification fails, the API returns:

```text
400 Bad Request
```

Example body:

```json
{
  "message": "CAPTCHA verification failed. Please try again.",
  "errors": ["invalid-input-response"]
}
```

## Adding CAPTCHA to a Future Endpoint

1. Add `CaptchaToken` to the request DTO.
2. Implement `ICaptchaProtectedRequest` on that DTO.
3. Add `[RequireCaptcha("your-action-name")]` to the controller action.
4. Render `TurnstileCaptcha` on the frontend form with the same action name.
5. Include the token in the request payload.

Use stable, specific action names such as `forgot-password`,
`contact-message`, or `public-comment`.
