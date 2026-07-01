# RBAC Permission Matrix

## Summary

The RBAC baseline defines four built-in roles:

```text
learner
content_manager
reviewer
admin
```

Content Manager and admin do not automatically inherit learner workflow permissions. Shared account permissions are intentionally assigned to all roles because every authenticated account needs basic account management.

## Shared authenticated account permissions

Assigned to: `learner`, `content_manager`, `reviewer`, `admin`

| Permission | Purpose |
|---|---|
| `account.view.self` | View own account/session summary |
| `account.update.self` | Update own account-level data |
| `auth_provider.view.self` | View own linked auth providers |
| `auth_provider.link.self` | Start linking an auth provider |
| `auth_provider.unlink.self` | Unlink own auth provider |
| `auth_provider.update.self` | Update own auth provider state, such as email changes |
| `profile.view.self` | View own profile metadata |
| `profile.update.self` | Update own profile metadata |

## Learner permissions

Assigned to: `learner`

### Account and portfolio

| Permission | Purpose |
|---|---|
| `account.delete.self` | Delete own account if supported |
| `portfolio.view.self` | View own portfolio management data |
| `portfolio.update.self` | Update own portfolio and selected repositories |

### GitHub and repository insight

| Permission | Purpose |
|---|---|
| `repository.view.self` | View own synced repositories |
| `repository.sync.self` | Sync own GitHub repositories |
| `repo_insight.view.self` | View own generated repository insights |
| `repo_insight.generate.self` | Generate AI repository insights for own repositories |

### AI credit and streak

| Permission | Purpose |
|---|---|
| `ai_credit.view.self` | View own AI credit status |
| `streak.view.self` | View own learning streak |
| `streak.track.self` | Track own learning activity |

### Roadmaps

| Permission | Purpose |
|---|---|
| `roadmap.view.published` | View published roadmaps inside the learner app |
| `roadmap_node.view.published` | View published roadmap node detail |
| `roadmap_enrollment.view.self` | View own roadmap enrollment |
| `roadmap_enrollment.create.self` | Enroll self into a roadmap |
| `roadmap_enrollment.migrate.self` | Migrate own roadmap enrollment to an approved minor/major update |
| `roadmap_progress.update.self` | Update own roadmap node progress |

### Learning modules

| Permission | Purpose |
|---|---|
| `learning_module.view.published` | View published learning modules inside the learner app |
| `learning_module_enrollment.view.self` | View own module enrollments |
| `learning_module_enrollment.create.self` | Enroll self into a module |
| `learning_module_lesson.view.enrolled` | View lessons for modules the learner is enrolled in |
| `learning_module_progress.update.self` | Update own lesson/module progress |
| `learning_module_quiz_attempt.view.self` | View own quiz attempts |
| `learning_module_quiz_attempt.create.self` | Start own quiz attempt |
| `learning_module_quiz_attempt.submit.self` | Submit own quiz attempt |
| `learning_module_chat.use.enrolled` | Use module AI assistant for enrolled modules |

### Discovery and analysis

| Permission | Purpose |
|---|---|
| `career_role.view.catalog` | View authenticated career-role catalog data |
| `skill_gap_analysis.create.self` | Run own skill-gap analysis |
| `skill_gap_analysis_history.view.self` | View own skill-gap analysis history |
| `skill_gap_analysis_history.delete.self` | Delete own skill-gap analysis history |
| `market_pulse.view.catalog` | View authenticated market pulse catalog data |
| `skill.view.catalog` | Search/view skills for authenticated app use |

## Content Manager permissions

Assigned to: `content_manager`

### Catalog lookup

| Permission | Purpose |
|---|---|
| `skill.view.catalog` | Search/view skills when creating or editing modules |
| `career_role.view.catalog` | View career-role catalog data when configuring content |
| `skill_gap_config.view.any` | View skill-gap configuration |
| `skill_gap_config.update.any` | Update skill-gap configuration |

Content Manager receives catalog skill lookup only. Content Manager should not receive `skill.view.any` because that is platform governance scope.

### Module ownership

| Permission | Purpose |
|---|---|
| `learning_module.view.own` | View own authored modules |
| `learning_module.create.own` | Create own modules |
| `learning_module.update.own` | Update own modules |
| `learning_module.delete.own` | Delete own modules where allowed |
| `learning_module.publish.own` | Publish own modules |
| `learning_module.archive.own` | Archive own modules |
| `learning_module.preview.own` | Preview own modules without creating learner progress |

### Lesson ownership

| Permission | Purpose |
|---|---|
| `learning_module_lesson.create.own` | Create/upload lessons for own modules |
| `learning_module_lesson.update.own` | Update lessons in own modules |
| `learning_module_lesson.delete.own` | Delete lessons in own modules |
| `learning_module_lesson.reorder.own` | Reorder lessons in own modules |
| `learning_module_lesson.reindex.own` | Reindex lesson content for own modules |

### Quiz ownership

| Permission | Purpose |
|---|---|
| `learning_module_quiz.view.own` | View quiz data for own modules |
| `learning_module_quiz.upsert.own` | Create or update quiz structure for own modules |
| `learning_module_quiz_question.create.own` | Create questions for own module quizzes |
| `learning_module_quiz_question.update.own` | Update questions for own module quizzes |
| `learning_module_quiz_question.delete.own` | Delete questions from own module quizzes |
| `learning_module_quiz_question.reorder.own` | Reorder questions in own module quizzes |

### Roadmap ownership and review submission

| Permission | Purpose |
|---|---|
| `roadmap_draft.view.own` | View roadmap list/detail scoped to `roadmap.owner_user_id = current_user_id` |
| `roadmap_draft.create.own` | Create new owned roadmaps and owned patch/minor/major update drafts |
| `roadmap_draft.update.own` | Edit owned roadmap metadata, nodes, requirements, skills, and resources |
| `roadmap_draft.delete.own` | Delete owned editable draft or changes-requested versions where allowed |
| `roadmap_review.submit.own` | Submit an owned editable draft to Reviewer with a changelog |

`own` roadmap permissions must be enforced by service-layer ownership checks against `roadmap.owner_user_id`. The permission only grants access to the content manager workflow. It does not grant access to another content manager's roadmaps.

## Reviewer permissions

Assigned to: `reviewer`

| Permission | Purpose |
|---|---|
| `roadmap_review.view.any` | View the roadmap review queue and review packet |
| `roadmap_review.approve.any` | Approve a pending version and publish it |
| `roadmap_review.reject.any` | Reject a pending version with a required reason |

## Admin permissions

Assigned to: `admin`

Admin is a platform governance role. It does not receive roadmap authoring or review permissions by default. If a real admin account must author roadmaps or review submissions, assign `content_manager` or `reviewer` explicitly in addition to `admin`.

The built-in `admin` role is protected. Platform governance screens and services must not allow the built-in admin role to be renamed, deleted, or stripped of permissions. An admin user also cannot revoke their own `admin` role. They can still revoke the `admin` role from another account when they have `user_role.revoke.any`.

### User governance

| Permission | Purpose |
|---|---|
| `user.view.any` | View platform users |
| `user.update.any` | Update platform users |
| `user.suspend.any` | Suspend users |
| `user.restore.any` | Restore users |
| `user.delete.any` | Delete users if supported |

### Role governance

| Permission | Purpose |
|---|---|
| `role.view.any` | View roles |
| `role.create.any` | Create roles |
| `role.update.any` | Update roles |
| `role.delete.any` | Delete roles |

### Permission governance

| Permission | Purpose |
|---|---|
| `permission.view.any` | View permissions |
| `permission.create.any` | Create permissions |
| `permission.update.any` | Update permissions |
| `permission.delete.any` | Delete permissions |

### Role-permission and user-role governance

| Permission | Purpose |
|---|---|
| `role_permission.view.any` | View role-permission assignments |
| `role_permission.assign.any` | Assign permissions to roles |
| `role_permission.revoke.any` | Revoke permissions from roles |
| `user_role.view.any` | View user-role assignments |
| `user_role.assign.any` | Assign roles to users |
| `user_role.revoke.any` | Revoke roles from users |

### Governance safeguards

| Rule | Behavior |
|---|---|
| Built-in admin role rename/delete | Blocked |
| Built-in admin permission removal | Blocked |
| Current user revoking own admin role | Blocked |
| Admin role revocation from another account | Allowed when the actor has `user_role.revoke.any` |
| Built-in/system permission rename/delete | Blocked |

### Skill and system governance

| Permission | Purpose |
|---|---|
| `skill.view.catalog` | Use authenticated skill list/search endpoint where reused by admin UI |
| `skill.view.any` | View skills in platform governance context |
| `skill.create.any` | Create skills |
| `skill.update.any` | Update skills |
| `skill.delete.any` | Delete skills |
| `market_pulse.manage.any` | Operate Market Pulse admin refreshes, retries, classifier mapping, and source health |
| `system_health.view.any` | View protected system/database health diagnostics |

## Explicit non-goals

| Role | Not granted by default |
|---|---|
| `content_manager` | Learner enrollment, learner progress, learner portfolio editing, platform user/role/permission governance |
| `reviewer` | Draft editing, learner enrollment, learner progress, platform user/role/permission governance |
| `admin` | Learner enrollment, learner progress, content manager module ownership actions, roadmap authoring/review actions, learner portfolio editing |

If a real user must operate in multiple product personas, assign multiple roles explicitly.
