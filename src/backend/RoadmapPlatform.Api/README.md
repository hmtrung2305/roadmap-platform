# RoadmapPlatform.Api

`RoadmapPlatform.Api` is the HTTP entry point for Roadmap Platform. It exposes
ASP.NET Core controllers, translates HTTP requests into application-service
calls, applies authentication and authorization, and converts failures into the
platform's standard error response.

Business rules belong in `RoadmapPlatform.Application`. Persistence, external
providers, and other technical integrations belong in
`RoadmapPlatform.Infrastructure`.

## Request pipeline

`Program.Main` composes the application from focused extension methods:

1. `AddApiServices` registers controllers, CORS, compression, API validation,
   and rate limiting.
2. `AddApiAuthentication` configures JWT, cookie, Google, and GitHub
   authentication.
3. `AddApiAuthorization` registers permission policies.
4. Application and infrastructure services are registered by their respective
   projects.
5. `UseApiPipeline` configures middleware and maps controllers.

Unhandled exceptions are converted by `ExceptionHandlingMiddleware`. Automatic
model-validation failures and manually created API errors use
`ApiErrorResponse`, so clients receive a consistent response shape.

## Controller areas

| Area | Route prefix | Responsibility |
| --- | --- | --- |
| Authentication | `/api/auth`, `/api/me/auth-providers` | Account registration, sign-in, token refresh, external providers, and account linking |
| Users | `/api/me`, `/api/admin/users`, `/api/portfolios`, `/api/streak` | Current-user profile, administration, portfolios, and activity streaks |
| Skills | `/api/skills`, `/api/content/skills` | Public skill lookup and content-manager skill maintenance |
| Skill gap | `/api/skill-gap/*`, `/api/me/skill-gap/*`, `/api/content/*` | Catalog, assessments, analysis, configuration, and history |
| Roadmaps | `/api/roadmaps`, `/api/roadmap-enrollments` | Published roadmaps, enrollment, and learner progress |
| Roadmap authoring | `/api/content/roadmaps`, `/api/content/roadmap-versions`, `/api/content/roadmap-nodes` | Drafting, validating, publishing, and maintaining roadmap structures |
| Learning resources | `/api/content/learning-resources` | Content-manager learning-resource catalog |
| Learning modules | `/api/learning-modules` | Module discovery, enrollment, lessons, progress, and quizzes |
| Learning-module authoring | `/api/content/learning-modules` | Module, lesson, and quiz authoring |
| Module assistant | `/api/learning-modules/{moduleId}/assistant/chat` | Context-grounded learner assistance |
| Market Pulse | `/api/market-pulse` | Public market analytics |
| Market Pulse operations | `/api/market-pulse/admin` | Refresh operations, imports, failures, mappings, and operational health |
| Market Pulse internal | `/api/internal/market-pulse` | Protected machine-to-machine refresh callbacks |
| AI mentor and credits | `/api/ai-mentor`, `/api/ai-credits` | Mentor conversations and AI-credit status |
| GitHub integration | `/api/integrations/github` | Repository synchronization and generated insights |
| Content workspace | `/api/content/workspace` | Content-manager dashboard summary |

The route attributes on each controller are the source of truth for individual
endpoint paths.

## Authentication and authorization

- Authentication identifies the caller and populates `HttpContext.User`.
- Permission-protected endpoints use `RequirePermissionAttribute` or
  `RequireAnyPermissionAttribute`.
- Controllers obtain the current user identifier through
  `ClaimsPrincipalExtensions`.
- Internal Market Pulse endpoints use their dedicated internal authentication
  contract and must not be exposed as public browser endpoints.
- Expensive and mutation endpoints use named policies from
  `RateLimitPolicyNames`.

Adding `[Authorize]` alone is not sufficient for a permission-controlled
operation; use the relevant permission attribute as well.

## Error contract

API errors use the following conceptual shape:

```json
{
  "code": "MACHINE_READABLE_CODE",
  "message": "Human-readable explanation.",
  "traceId": "request-correlation-id",
  "errors": {
    "field": ["Validation message."]
  }
}
```

Controllers should return the most specific HTTP status available. Domain and
application exceptions that are intentionally handled by
`ExceptionHandlingMiddleware` should not be caught again in controllers unless
the endpoint must translate them into a different public contract.

## Adding an endpoint

1. Add the request/response DTO and use case to the Application project.
2. Inject the application service into the relevant controller.
3. Add the HTTP method and route attributes.
4. Apply the required permission and rate-limit policy.
5. Document the action with an XML `<summary>` and document non-obvious
   parameters or behavior.
6. Declare expected response types with `ProducesResponseType`.
7. Add controller or system tests for success, validation, authorization, and
   conflict/not-found behavior as applicable.

Controllers should remain thin: validate the HTTP contract, identify the caller,
invoke one application use case, and translate its result.

## Local development

From the repository root:

```powershell
dotnet restore src/backend/RoadmapPlatform.Api/RoadmapPlatform.Api.csproj
dotnet run --project src/backend/RoadmapPlatform.Api/RoadmapPlatform.Api.csproj
```

Use `appsettings.Development.json`, environment variables, or .NET user secrets
for local configuration. Never commit access tokens, OAuth secrets, database
passwords, or internal API keys.

Build and test:

```powershell
dotnet build src/backend/RoadmapPlatform.Api/RoadmapPlatform.Api.csproj
dotnet test tests/backend/RoadmapPlatform.Tests/RoadmapPlatform.Tests.csproj
```

The project generates an XML documentation file beside the compiled assembly.
Keep XML comments accurate whenever an endpoint contract changes.
