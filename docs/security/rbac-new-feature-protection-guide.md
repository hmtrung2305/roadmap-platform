# Protecting New Features with RBAC

## Purpose

This document explains how to add RBAC protection when the Roadmap Platform gains a new feature, endpoint, page, or UI action.

It does not define how to run database seeds. Seed execution belongs to the database workflow. This guide only explains when RBAC seed contents need to change and how to keep backend, frontend, and documentation aligned.

## Core rule

Every new feature must answer these questions before implementation:

```text
Who is this feature for?
Which surface owns it?
Which permission protects it?
Does the action operate on self, own, published, catalog, or any scope?
Does the service need an ownership check in addition to the permission check?
Should the frontend hide the route/action for users without permission?
```

Do not add an endpoint or page first and "figure out RBAC later." The permission model is part of the feature design.

## Product personas and surfaces

The platform uses separate personas, not automatic role inheritance.

| Persona | Surface | Purpose |
|---|---|---|
| `learner` | Learner app | Uses learning workflows |
| `counselor` | Counselor console | Manages authored learning content |
| `admin` | Admin console | Manages platform governance |

A user can hold multiple roles, but one role does not automatically inherit another role's feature domain.

Incorrect assumption:

```text
admin includes counselor includes learner
```

Correct assumption:

```text
learner, counselor, and admin are separate permission domains
```

## Permission naming convention

Permissions use this format:

```text
resource.action.scope
```

Examples:

```text
learning_module.update.own
roadmap_enrollment.create.self
skill.view.catalog
user_role.assign.any
```

### Resource

The `resource` should match the business object being protected.

Good:

```text
learning_module
learning_module_lesson
learning_module_quiz_question
roadmap_enrollment
user_role
system_health
```

Avoid vague resources:

```text
management
admin
content
stuff
```

### Action

Use a concrete verb that matches the operation.

Common actions:

```text
view
create
update
delete
publish
archive
assign
revoke
submit
sync
generate
use
reorder
reindex
```

Avoid overloaded actions like `manage` unless the endpoint is truly coarse-grained. Prefer separate permissions when actions have different risk levels.

### Scope

Scope defines the boundary of authority.

| Scope | Meaning | Example |
|---|---|---|
| `self` | Current user's own account/activity | `portfolio.update.self` |
| `own` | Resource authored/owned by current user | `learning_module.update.own` |
| `published` | Published content available inside the authenticated learner app | `roadmap.view.published` |
| `catalog` | Authenticated lookup/list data used by app workflows | `skill.view.catalog` |
| `enrolled` | Content accessible through a user's enrollment | `learning_module_lesson.view.enrolled` |
| `any` | Platform-wide access, usually admin only | `user.update.any` |

Do not use `public` unless the feature is intentionally anonymous. In this project, only auth entrypoints and public portfolio are anonymous.

## Backend implementation checklist

### 1. Add the permission constant

Add a constant in the backend permission constants file.

Example:

```csharp
public const string LEARNING_MODULE_REVIEW_OWN = "learning_module.review.own";
```

The constant value must match the permission in the RBAC seed data.

### 2. Protect the controller action

Use `RequirePermission`, not role checks.

```csharp
[RequirePermission(PermissionConstant.LEARNING_MODULE_UPDATE_OWN)]
[HttpPatch("{moduleId:guid}")]
public async Task<ActionResult> UpdateModule(Guid moduleId, UpdateModuleRequest request)
{
    ...
}
```

Avoid this pattern:

```csharp
[Authorize(Roles = "admin")]
```

Role checks hard-code policy into controllers and make the permission matrix harder to evolve.

### 3. Keep ownership checks in the service layer

A permission with `own` or `self` does not prove ownership by itself.

Controller permission check:

```text
Does the user have learning_module.update.own?
```

Service ownership check:

```text
Does this module belong to the current counselor?
```

Example rule:

```text
learning_module.update.own allows update only when created_by_user_id == current_user_id
```

For `any` permissions, the service may allow platform-wide access, but only if the controller requires the correct `*.any` permission.

### 4. Decide whether `own` and `any` need separate endpoints or branching

Some features only need one scope.

Example:

```text
Counselor edits own module only -> learning_module.update.own
```

Some features need both own-scope and admin-scope behavior.

Example:

```text
Counselor archives own module -> learning_module.archive.own
Admin moderates any module -> learning_module.archive.any
```

Do not grant `*.any` to counselor just to avoid writing ownership logic.

### 5. Preserve correct API status semantics

Backend APIs should return proper auth semantics:

| Case | API result |
|---|---|
| No valid token | `401 Unauthorized` |
| Valid token, missing permission | `403 Forbidden` |
| Valid permission but missing ownership | Usually `404 Not Found` or domain-specific not-found response |

The frontend may show a neutral not-found page for unauthorized routes, but the backend should still enforce and report authorization correctly.

## Frontend implementation checklist

### 1. Add the permission constant

Add the permission to the frontend permission constants file.

Example:

```js
LEARNING_MODULE_REVIEW_OWN: "learning_module.review.own"
```

Frontend constants must match backend constants and seed values exactly.

### 2. Guard route groups by surface

Use surface-level route guards for major route groups.

```jsx
<RequirePermission anyPermissions={COUNSELOR_SURFACE_PERMISSIONS}>
  <CounselorLayout />
</RequirePermission>
```

Surface guards decide whether a user can enter a whole area, such as learner app, counselor console, or admin console.

### 3. Guard sensitive actions by action permission

Surface access is not enough for every button.

Example:

```jsx
{hasPermission(user, PERMISSIONS.LEARNING_MODULE_PUBLISH_OWN) && (
  <button>Publish</button>
)}
```

Use action-level checks for buttons, tabs, destructive actions, and management links.

### 4. Hide navigation for unavailable surfaces

Do not render counselor/admin navigation links for users without matching permissions.

Expected behavior:

```text
learner -> no counselor/admin nav
counselor -> no learner/admin nav unless also granted those permissions
admin -> no learner/counselor nav unless also granted those permissions
```

### 5. Use neutral not-found for wrong-surface routes

Frontend route behavior:

| User state | Route result |
|---|---|
| Anonymous opens protected route | Redirect to login |
| Authenticated but wrong surface | Neutral not-found page |
| Authenticated and authorized | Render route |

Do not show a dedicated "you do not have permission" page for hidden staff/admin surfaces.

## RBAC seed update rules

Seed execution is handled by the database workflow. When a feature introduces a new permission, update the RBAC seed contents according to the current database seed convention.

The seed should define:

```text
new permissions
role-permission mappings for built-in roles
```

For the first RBAC version, the baseline seed is allowed to converge the built-in roles back to the canonical mapping.

For future RBAC versions, prefer additive or versioned seed changes according to the database workflow.

Do not put environment-specific users in the core RBAC seed:

```text
personal user accounts
test accounts
God/all-roles account
sample portfolios
sample module data
```

Those belong to dev/local seeds.

## Common feature patterns

### Learner feature

Example: learner bookmarks a roadmap node.

Permissions:

```text
roadmap_bookmark.view.self
roadmap_bookmark.create.self
roadmap_bookmark.delete.self
```

Role mapping:

```text
learner -> roadmap_bookmark.view.self
learner -> roadmap_bookmark.create.self
learner -> roadmap_bookmark.delete.self
```

Backend:

```csharp
[RequirePermission(PermissionConstant.ROADMAP_BOOKMARK_CREATE_SELF)]
```

Service check:

```text
Bookmark must be created for current_user_id only.
```

Frontend:

```text
Show bookmark UI only inside learner surface.
```

### Counselor feature

Example: counselor reviews generated module chunks for one of their modules.

Permissions:

```text
learning_module_chunk.view.own
learning_module_chunk.update.own
```

Role mapping:

```text
counselor -> learning_module_chunk.view.own
counselor -> learning_module_chunk.update.own
```

Service check:

```text
Module created_by_user_id must equal current counselor user id.
```

Do not grant:

```text
learning_module_chunk.update.any
```

unless admins need platform-wide moderation over all module chunks.

### Admin feature

Example: admin manages user role assignments.

Permissions:

```text
user_role.view.any
user_role.assign.any
user_role.revoke.any
```

Role mapping:

```text
admin -> user_role.view.any
admin -> user_role.assign.any
admin -> user_role.revoke.any
```

Frontend:

```text
Route under /admin/*
Guard with ADMIN_SURFACE_PERMISSIONS
Hide from learner/counselor users
```

## Pre-merge checklist for new features

Before merging a new feature, verify:

```text
[ ] The owning persona/surface is clear.
[ ] Every new protected endpoint has RequirePermission or explicit AllowAnonymous.
[ ] Anonymous access is intentional and documented.
[ ] New permissions follow resource.action.scope.
[ ] Backend constants, seed values, and frontend constants match exactly.
[ ] self/own/enrolled scopes are enforced in services, not only in permission names.
[ ] Frontend route groups are guarded by surface permissions.
[ ] Sensitive UI actions are hidden by action-level permissions.
[ ] Wrong-surface access renders neutral not-found in the frontend.
[ ] /api/me returns the permissions needed by the frontend.
[ ] Permission matrix and route access docs are updated.
```

## Review questions

Use these questions during code review:

```text
Could a user without this role discover or call the endpoint?
Could a counselor act on another counselor's resource?
Could an admin accidentally participate in learner workflows?
Does any endpoint rely on frontend hiding instead of backend enforcement?
Does the permission name describe the actual business action?
Is the scope too broad for the feature?
Does the seed grant only what the role needs?
```

If any answer is unclear, the feature is not ready for merge.
