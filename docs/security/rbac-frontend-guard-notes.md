# Frontend RBAC Guard Notes

## Guard components

The frontend uses these guard concepts:

| Component/helper | Responsibility |
|---|---|
| `PublicRoute` | Prevents authenticated users from staying on login/register style pages |
| `RequirePermission` | Requires authentication and at least one/all permissions depending on props |
| `NotFoundPage` | Neutral response for unknown routes and unauthorized authenticated route access |
| `hasPermission` | Checks one permission |
| `hasAnyPermission` | Checks at least one permission |
| `hasAllPermissions` | Checks every listed permission |

## Unauthorized route behavior

The frontend intentionally avoids showing a forbidden page for hidden surfaces.

Expected behavior:

```text
Unauthenticated protected route -> login
Authenticated but wrong surface -> not found
Unknown route -> not found
```

This prevents the UI from confirming staff/admin route existence to normal users. Backend API responses should still use correct `401` and `403` semantics.

## Surface permission sets

Surface sets are broad entry checks:

```text
LEARNER_SURFACE_PERMISSIONS
COUNSELOR_SURFACE_PERMISSIONS
ADMIN_SURFACE_PERMISSIONS
```

They should be used to guard route groups. They should not replace action-level permission checks for sensitive UI controls.

Examples:

```jsx
<RequirePermission anyPermissions={LEARNER_SURFACE_PERMISSIONS}>
  <MainLayout />
</RequirePermission>
```

```jsx
<RequirePermission anyPermissions={COUNSELOR_SURFACE_PERMISSIONS}>
  <CounselorLayout />
</RequirePermission>
```

```jsx
<RequirePermission anyPermissions={ADMIN_SURFACE_PERMISSIONS}>
  <AdminLayout />
</RequirePermission>
```

## Navigation visibility

Navigation links should be hidden when the current user lacks the relevant surface permission.

Examples:

| Link | Show when |
|---|---|
| Learner app links | user has any learner surface permission |
| Counselor console link | user has any counselor surface permission |
| Admin console link | user has any admin surface permission |

Do not rely on hidden links as security. The route guard and backend permission checks are still required.

## Post-login redirect

After login, the frontend must fetch `/api/me` before resolving the redirect because login response data is not the source of truth for effective permissions.

Default redirect priority:

```text
admin -> counselor -> learner
```

Current defaults:

```text
admin     = /admin
counselor = /counselor/learning-modules
learner   = /roadmaps
```

If a user attempted to open a route before login, redirect back only when the user can access that route. Otherwise, redirect to the user's default authorized surface.

## Public portfolio exception

The public portfolio route is intentionally outside learner surface protection:

```text
/portfolio/:username
/portfolios/:username
```

But learner-owned portfolio management remains protected:

```text
/portfolio
/portfolio/edit
/portfolio/repositories
```

The public route helper must distinguish these paths so `/portfolio/edit` and `/portfolio/repositories` are not accidentally treated as public profile URLs.
