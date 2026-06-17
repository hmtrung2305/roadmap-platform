# RBAC Permission Matrix

## Summary

The first RBAC version defines three built-in roles:

```text
learner
counselor
admin
```

Counselor and admin do not automatically inherit learner workflow permissions. Shared account permissions are intentionally assigned to all roles because every authenticated account needs basic account management.

## Shared authenticated account permissions

Assigned to: `learner`, `counselor`, `admin`

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
| `market_pulse.view.catalog` | View authenticated market pulse catalog data |
| `skill.view.catalog` | Search/view skills for authenticated app use |

## Counselor permissions

Assigned to: `counselor`

### Catalog lookup

| Permission | Purpose |
|---|---|
| `skill.view.catalog` | Search/view skills when creating or editing modules |

Counselor receives catalog skill lookup only. Counselor should not receive `skill.view.any` because that is platform governance scope.

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

## Admin permissions

Assigned to: `admin`

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

### Skill and system governance

| Permission | Purpose |
|---|---|
| `skill.view.catalog` | Use authenticated skill list/search endpoint where reused by admin UI |
| `skill.view.any` | View skills in platform governance context |
| `skill.create.any` | Create skills |
| `skill.update.any` | Update skills |
| `skill.delete.any` | Delete skills |
| `system_health.view.any` | View protected system/database health diagnostics |

## Explicit non-goals

| Role | Not granted by default |
|---|---|
| `counselor` | Learner enrollment, learner progress, learner portfolio editing, platform user/role/permission governance |
| `admin` | Learner enrollment, learner progress, counselor module ownership actions, learner portfolio editing |

If a real user must operate in multiple product personas, assign multiple roles explicitly.
