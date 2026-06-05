# AI Credit Limits

The AI Mentor uses a daily credit limit to control prompt usage while the
project is still experimental.

## Default Behavior

- Authenticated users use the `free` plan by default.
- The `free` plan allows `5` AI chat prompts per UTC day.
- One successful AI Mentor response costs `1` credit.
- Invalid requests and failed AI calls are not charged.
- When the user reaches the daily limit, `POST /api/chat` returns `429`.

## Endpoints

### Get Current Credit Status

```http
GET /api/chat/credits
Authorization: Bearer <token>
```

Response:

```json
{
  "planCode": "free",
  "dailyCreditLimit": 5,
  "usedCreditsToday": 3,
  "remainingCreditsToday": 2,
  "resetAt": "2026-06-06T00:00:00+00:00"
}
```

### Chat With AI Mentor

```http
POST /api/chat
Authorization: Bearer <token>
Content-Type: application/json
```

Each successful response includes the updated `credits` object.

If the limit is reached:

```json
{
  "message": "Daily AI credit limit reached.",
  "credits": {
    "planCode": "free",
    "dailyCreditLimit": 5,
    "usedCreditsToday": 5,
    "remainingCreditsToday": 0,
    "resetAt": "2026-06-07T00:00:00+00:00"
  }
}
```

## Plans

Plans are stored in `public.ai_credit_plan`.

| Plan | Daily Limit | Intended Use |
| --- | ---: | --- |
| `free` | 5 | Default users |
| `premium` | 100 | Future paid users |
| `admin` | 1000 | Internal testing and administrators |

## Assigning Premium Or Admin

Default users do not need a row in `public.user_ai_credit_plan`. To upgrade a
user, insert or update one row:

```sql
INSERT INTO public.user_ai_credit_plan (user_id, plan_code)
VALUES ('USER_ID_HERE', 'premium')
ON CONFLICT (user_id) DO UPDATE
SET plan_code = EXCLUDED.plan_code,
    updated_at = now();
```

To make the upgrade temporary:

```sql
INSERT INTO public.user_ai_credit_plan (user_id, plan_code, expires_at)
VALUES ('USER_ID_HERE', 'premium', now() + interval '30 days')
ON CONFLICT (user_id) DO UPDATE
SET plan_code = EXCLUDED.plan_code,
    expires_at = EXCLUDED.expires_at,
    updated_at = now();
```

## Database Setup

For a new database, run `docs/database/schema.sql`.

For an existing database, run:

```bat
psql -U postgres -d roadmap_platform -f docs\database\migrations\2026-06-06_ai_credit_limits.sql
```