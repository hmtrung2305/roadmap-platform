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
| `content_manager` | Content Manager console | Manages authored learning content |
| `admin` | Admin console | Manages platform governance |

A user can hold multiple roles, but one role does not automatically inherit another role's feature domain.

Incorrect assumption:

```text
admin includes content manager includes learner
```

Correct assumption:

```text
learner, content manager, and admin are separate permission domains
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
Does this module belong to the current content manager?
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
Content Manager edits own module only -> learning_module.update.own
```

Some features need both own-scope and admin-scope behavior.

Example:

```text
Content Manager archives own module -> learning_module.archive.own
Admin moderates any module -> learning_module.archive.any
```

Do not grant `*.any` to content manager just to avoid writing ownership logic.

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
<RequirePermission anyPermissions={CONTENT_MANAGER_SURFACE_PERMISSIONS}>
  <ContentManagerLayout />
</RequirePermission>
```

Surface guards decide whether a user can enter a whole area, such as learner app, content manager console, or admin console.

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

Do not render content manager/admin navigation links for users without matching permissions.

Expected behavior:

```text
learner -> no content manager/admin nav
content manager -> no learner/admin nav unless also granted those permissions
admin -> no learner/content nav unless also granted those permissions
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

Do not put environment-specific users in the core RBAC seed:

```text
personal user accounts
test accounts
God/all-roles account
sample portfolios
sample module data
```

Those belong to dev/local seeds.

## Adding new permissions to SQL seeds

Treat the first RBAC seed as the baseline.

Example baseline file:

```text
rbac-roles-permissions.seed.sql
```

After that file has been committed, shared, or applied to another environment, do not keep expanding it for every new feature. Add a new incremental seed file instead.

Good:

```text
001-rbac-roles-permissions.seed.sql
002-rbac-learning-module-review-permissions.seed.sql
003-rbac-roadmap-moderation-permissions.seed.sql
```

Also acceptable if the project does not use numeric prefixes yet:

```text
rbac-roles-permissions.seed.sql
rbac-learning-module-review-permissions.seed.sql
rbac-roadmap-moderation-permissions.seed.sql
```

Update the existing baseline seed only when the change is still local and the seed has not been merged, shared, or applied outside the current working database.

### What the new SQL seed should contain

A permission seed for a new feature should usually include two parts:

```text
1. Insert the new permissions.
2. Insert the role-permission mappings for built-in roles.
```

Example:

```sql
INSERT INTO permissions (name, description)
VALUES
    ('learning_module_review.view.own', 'View review data for own learning modules'),
    ('learning_module_review.update.own', 'Update review data for own learning modules')
ON CONFLICT (name) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON p.name IN (
    'learning_module_review.view.own',
    'learning_module_review.update.own'
)
WHERE r.name = 'content_manager'
ON CONFLICT (role_id, permission_id) DO NOTHING;
```

The exact table and column names must match the database schema, but the rule is the same: permission inserts and role-permission inserts must be safe to run more than once.

### Required properties

New RBAC SQL seed files should be:

```text
idempotent
ordered after the seed they depend on
limited to RBAC data
named after the feature or permission area
safe to run on a database that already has older RBAC seeds applied
```

Avoid destructive changes in incremental permission seeds unless the feature explicitly requires a permission migration.

Avoid:

```sql
DELETE FROM role_permissions;
DELETE FROM permissions;
```

Prefer targeted additive changes:

```sql
INSERT ...
ON CONFLICT DO NOTHING;
```

If a permission name is wrong and has already been shared, create a follow-up correction seed instead of silently changing the old seed file. The follow-up seed should add the corrected permission, map it to the correct roles, and only remove or unmap the old permission if the application no longer uses it.

Backend constants, frontend constants, and SQL seed values must match exactly.

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

### Content Manager feature

Example: content manager reviews generated module chunks for one of their modules.

Permissions:

```text
learning_module_chunk.view.own
learning_module_chunk.update.own
```

Role mapping:

```text
content_manager -> learning_module_chunk.view.own
content_manager -> learning_module_chunk.update.own
```

Service check:

```text
Module created_by_user_id must equal current content manager user id.
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
Hide from learner/content manager users
```
