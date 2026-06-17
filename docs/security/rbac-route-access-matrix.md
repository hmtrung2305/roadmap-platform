# RBAC Route Access Matrix

## Frontend route surfaces

### Public routes

| Route | Access | Notes |
|---|---|---|
| `/` | Anonymous | Landing page |
| `/login` | Anonymous only through `PublicRoute` | Authenticated users redirect to their default authorized surface |
| `/register` | Anonymous only through `PublicRoute` | Authenticated users redirect to their default authorized surface |
| `/verify-email` | Anonymous only through `PublicRoute` | Authenticated users redirect to their default authorized surface |
| `/portfolio/:username` | Anonymous | Public portfolio |
| `/portfolios/:username` | Anonymous | Public portfolio alias |

### Learner surface

Guard: `RequirePermission(anyPermissions={LEARNER_SURFACE_PERMISSIONS})`

| Route | Purpose |
|---|---|
| `/roadmaps` | Roadmap selection |
| `/roadmaps/:slug` | Roadmap viewer |
| `/learning-modules` | Learner module dashboard |
| `/learning-modules/browse` | Browse published modules |
| `/learning-modules/:slug/overview` | Published module overview |
| `/learning-modules/:slug/study` | Module reader/study room |
| `/learning-modules/:slug` | Module reader alias |
| `/portfolio` | Own portfolio view/edit entry |
| `/portfolio/edit` | Edit own portfolio |
| `/portfolio/repositories` | Manage own portfolio repositories |
| `/profile` | Own profile page |
| `/market-pulse` | Market pulse dashboard |
| `/skill-gap` | Skill gap analysis |
| `/skill-gap-analysis` | Skill gap analysis alias |
| `/settings/*` | Learner account/settings pages |

### Content Manager surface

Guard: `RequirePermission(anyPermissions={CONTENT_MANAGER_SURFACE_PERMISSIONS})`

| Route | Purpose |
|---|---|
| `/content` | Redirects to `/content/learning-modules` |
| `/content/learning-modules` | Own module management list |
| `/content/learning-modules/create` | Create module draft |
| `/content/learning-modules/:moduleSlug/edit` | Edit own module |
| `/content/learning-modules/:moduleSlug/preview` | Preview own module as learner-facing content without learner progress |
| `/content/settings` | Content Manager console settings |

### Admin surface

Guard: `RequirePermission(anyPermissions={ADMIN_SURFACE_PERMISSIONS})`

| Route | Purpose |
|---|---|
| `/admin` | Admin console shell/home |
| `/admin/settings` | Admin console settings |

Future admin pages should be added under `/admin/*`, for example:

```text
/admin/users
/admin/roles
/admin/permissions
/admin/skills
```

## Frontend unauthorized behavior

| User type | Example route | Result |
|---|---|---|
| Anonymous | `/roadmaps` | Redirect to `/login` |
| Learner | `/content/learning-modules` | Not-found page |
| Learner | `/admin` | Not-found page |
| Content Manager | `/roadmaps` | Not-found page |
| Content Manager | `/admin` | Not-found page |
| Admin | `/roadmaps` | Not-found page |
| Admin | `/content/learning-modules` | Not-found page |
| Multi-role test account | Any granted surface | Allowed |

## Backend route access

### Anonymous API endpoints

| Endpoint group | Access | Notes |
|---|---|---|
| Auth login/register/verification | Anonymous | Required to create/login accounts |
| OAuth login/callback endpoints | Anonymous | External provider redirects require anonymous callback access |
| OAuth linking callbacks | Anonymous callback only | Link start endpoint remains authenticated |
| Public portfolio | Anonymous | Only public user-facing resource |

### Shared current-user endpoints

| Endpoint group | Permission |
|---|---|
| Current user authorization summary | `account.view.self` |
| Own profile/account updates | `profile.update.self`, `account.update.self`, or related self-scope permissions |
| Own auth provider management | `auth_provider.*.self` |

### Learner API areas

| Area | Main permissions |
|---|---|
| Roadmap catalog/detail | `roadmap.view.published`, `roadmap_node.view.published` |
| Roadmap enrollment/progress | `roadmap_enrollment.*.self`, `roadmap_progress.update.self` |
| Published learning modules | `learning_module.view.published` |
| Module enrollment/progress | `learning_module_enrollment.*.self`, `learning_module_progress.update.self` |
| Quiz attempts | `learning_module_quiz_attempt.*.self` |
| Module AI assistant | `learning_module_chat.use.enrolled` |
| Portfolio management | `portfolio.*.self` |
| GitHub repositories | `repository.*.self` |
| Repository insights | `repo_insight.*.self` |
| AI credits | `ai_credit.view.self` |
| Streaks | `streak.*.self` |
| Market/discovery | `career_role.view.catalog`, `market_pulse.view.catalog`, `skill.view.catalog`, `skill_gap_analysis.create.self` |

### Content Manager API areas

| Area | Main permissions |
|---|---|
| Module list/create/update/delete | `learning_module.*.own` |
| Module publish/archive/preview | `learning_module.publish.own`, `learning_module.archive.own`, `learning_module.preview.own` |
| Lesson management | `learning_module_lesson.*.own` |
| Quiz management | `learning_module_quiz.*.own`, `learning_module_quiz_question.*.own` |
| Skill search in module authoring | `skill.view.catalog` |

Ownership is still enforced in the service layer.

### Admin API areas

| Area | Main permissions |
|---|---|
| Role management | `role.*.any` |
| Permission management | `permission.*.any` |
| Role-permission assignment | `role_permission.assign.any`, `role_permission.revoke.any` |
| Future user-role assignment | `user_role.*.any` |
| Future user management | `user.*.any` |
| Future skill governance | `skill.*.any` |
| System health diagnostics | `system_health.view.any` |

## Legacy route cleanup status

Legacy module-management routes under `/admin/learning-modules` should not exist after the content manager/admin split.

Expected current routes:

```text
/content/learning-modules
/content/learning-modules/create
/content/learning-modules/:moduleSlug/edit
/content/learning-modules/:moduleSlug/preview
```

Do not reintroduce `AdminLearningModule*` component names unless they are truly admin governance pages.
