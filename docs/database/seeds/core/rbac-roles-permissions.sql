-- =====================================================================
-- RBAC Core Seed: Roles, Permissions, and Role-Permission Assignments
-- =====================================================================
-- Purpose:
--   Seeds the core RBAC data used by the Roadmap Platform.
--
-- Model:
--   - learner   = learning participant
--   - counselor = learning content operator
--   - admin     = platform operator
--
-- Important product rule:
--   Roles are separate product personas. Counselor and admin do not
--   automatically inherit learner workflow permissions.
--
-- Permission naming convention:
--   resource.action.scope
--
-- Scope meanings used here:
--   - self      = the authenticated user's own account/activity
--   - own       = resource owned by the authenticated user, usually author-owned content
--   - any       = platform-level access across users/owners
--   - published = published content visible inside the authenticated learner app
--   - enrolled  = content available through an existing learner enrollment
--   - catalog   = authenticated read-only catalog/discovery data

-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- Roles
-- ---------------------------------------------------------------------
INSERT INTO public.role (role_name)
VALUES
    ('learner'),
    ('counselor'),
    ('admin')
ON CONFLICT (role_name) DO NOTHING;

-- ---------------------------------------------------------------------
-- Permissions
-- ---------------------------------------------------------------------
INSERT INTO public.permission (permission_name)
VALUES
    -- Shared authenticated account permissions
    ('account.view.self'),
    ('account.update.self'),
    ('auth_provider.view.self'),
    ('auth_provider.link.self'),
    ('auth_provider.unlink.self'),
    ('auth_provider.update.self'),
    ('profile.view.self'),
    ('profile.update.self'),

    -- Learner account and portfolio permissions
    ('account.delete.self'),
    ('portfolio.view.self'),
    ('portfolio.update.self'),

    -- Learner GitHub/repository permissions
    ('repository.view.self'),
    ('repository.sync.self'),
    ('repo_insight.view.self'),
    ('repo_insight.generate.self'),

    -- Learner AI credit and streak permissions
    ('ai_credit.view.self'),
    ('streak.view.self'),
    ('streak.track.self'),

    -- Learner roadmap permissions
    ('roadmap.view.published'),
    ('roadmap_node.view.published'),
    ('roadmap_enrollment.view.self'),
    ('roadmap_enrollment.create.self'),
    ('roadmap_progress.update.self'),

    -- Learner learning module permissions
    ('learning_module.view.published'),
    ('learning_module_enrollment.view.self'),
    ('learning_module_enrollment.create.self'),
    ('learning_module_lesson.view.enrolled'),
    ('learning_module_progress.update.self'),
    ('learning_module_quiz_attempt.view.self'),
    ('learning_module_quiz_attempt.create.self'),
    ('learning_module_quiz_attempt.submit.self'),
    ('learning_module_chat.use.enrolled'),

    -- Learner discovery and analysis permissions
    ('career_role.view.catalog'),
    ('skill_gap_analysis.create.self'),
    ('market_pulse.view.catalog'),
    ('skill.view.catalog'),

    -- Counselor skill lookup permissions
    ('skill.view.any'),

    -- Counselor learning module ownership permissions
    ('learning_module.view.own'),
    ('learning_module.create.own'),
    ('learning_module.update.own'),
    ('learning_module.delete.own'),
    ('learning_module.publish.own'),
    ('learning_module.archive.own'),
    ('learning_module.preview.own'),

    -- Counselor lesson ownership permissions
    ('learning_module_lesson.create.own'),
    ('learning_module_lesson.update.own'),
    ('learning_module_lesson.delete.own'),
    ('learning_module_lesson.reorder.own'),
    ('learning_module_lesson.reindex.own'),

    -- Counselor quiz ownership permissions
    ('learning_module_quiz.view.own'),
    ('learning_module_quiz.upsert.own'),
    ('learning_module_quiz_question.create.own'),
    ('learning_module_quiz_question.update.own'),
    ('learning_module_quiz_question.delete.own'),
    ('learning_module_quiz_question.reorder.own'),

    -- Admin user governance permissions
    ('user.view.any'),
    ('user.update.any'),
    ('user.suspend.any'),
    ('user.restore.any'),
    ('user.delete.any'),

    -- Admin role governance permissions
    ('role.view.any'),
    ('role.create.any'),
    ('role.update.any'),
    ('role.delete.any'),

    -- Admin permission governance permissions
    ('permission.view.any'),
    ('permission.create.any'),
    ('permission.update.any'),
    ('permission.delete.any'),

    -- Admin role-permission and user-role governance permissions
    ('role_permission.view.any'),
    ('role_permission.assign.any'),
    ('role_permission.revoke.any'),
    ('user_role.view.any'),
    ('user_role.assign.any'),
    ('user_role.revoke.any'),

    -- Admin system governance permissions
    ('system_health.view.any'),

    -- Admin skill governance permissions
    ('skill.create.any'),
    ('skill.update.any'),
    ('skill.delete.any')
ON CONFLICT (permission_name) DO NOTHING;

-- ---------------------------------------------------------------------
-- Role-permission assignments
-- ---------------------------------------------------------------------
WITH role_permissions(role_name, permission_name) AS (
    VALUES
        -- Shared authenticated account permissions
        ('learner', 'account.view.self'),
        ('learner', 'account.update.self'),
        ('learner', 'auth_provider.view.self'),
        ('learner', 'auth_provider.link.self'),
        ('learner', 'auth_provider.unlink.self'),
        ('learner', 'auth_provider.update.self'),
        ('learner', 'profile.view.self'),
        ('learner', 'profile.update.self'),

        ('counselor', 'account.view.self'),
        ('counselor', 'account.update.self'),
        ('counselor', 'auth_provider.view.self'),
        ('counselor', 'auth_provider.link.self'),
        ('counselor', 'auth_provider.unlink.self'),
        ('counselor', 'auth_provider.update.self'),
        ('counselor', 'profile.view.self'),
        ('counselor', 'profile.update.self'),

        ('admin', 'account.view.self'),
        ('admin', 'account.update.self'),
        ('admin', 'auth_provider.view.self'),
        ('admin', 'auth_provider.link.self'),
        ('admin', 'auth_provider.unlink.self'),
        ('admin', 'auth_provider.update.self'),
        ('admin', 'profile.view.self'),
        ('admin', 'profile.update.self'),

        -- Learner account and portfolio permissions
        ('learner', 'account.delete.self'),
        ('learner', 'portfolio.view.self'),
        ('learner', 'portfolio.update.self'),

        -- Learner GitHub/repository permissions
        ('learner', 'repository.view.self'),
        ('learner', 'repository.sync.self'),
        ('learner', 'repo_insight.view.self'),
        ('learner', 'repo_insight.generate.self'),

        -- Learner AI credit and streak permissions
        ('learner', 'ai_credit.view.self'),
        ('learner', 'streak.view.self'),
        ('learner', 'streak.track.self'),

        -- Learner roadmap permissions
        ('learner', 'roadmap.view.published'),
        ('learner', 'roadmap_node.view.published'),
        ('learner', 'roadmap_enrollment.view.self'),
        ('learner', 'roadmap_enrollment.create.self'),
        ('learner', 'roadmap_progress.update.self'),

        -- Learner learning module permissions
        ('learner', 'learning_module.view.published'),
        ('learner', 'learning_module_enrollment.view.self'),
        ('learner', 'learning_module_enrollment.create.self'),
        ('learner', 'learning_module_lesson.view.enrolled'),
        ('learner', 'learning_module_progress.update.self'),
        ('learner', 'learning_module_quiz_attempt.view.self'),
        ('learner', 'learning_module_quiz_attempt.create.self'),
        ('learner', 'learning_module_quiz_attempt.submit.self'),
        ('learner', 'learning_module_chat.use.enrolled'),

        -- Learner discovery and analysis permissions
        ('learner', 'career_role.view.catalog'),
        ('learner', 'skill_gap_analysis.create.self'),
        ('learner', 'market_pulse.view.catalog'),
        ('learner', 'skill.view.catalog'),

        -- Counselor content management permissions
        ('counselor', 'skill.view.any'),
        ('counselor', 'learning_module.view.own'),
        ('counselor', 'learning_module.create.own'),
        ('counselor', 'learning_module.update.own'),
        ('counselor', 'learning_module.delete.own'),
        ('counselor', 'learning_module.publish.own'),
        ('counselor', 'learning_module.archive.own'),
        ('counselor', 'learning_module.preview.own'),
        ('counselor', 'learning_module_lesson.create.own'),
        ('counselor', 'learning_module_lesson.update.own'),
        ('counselor', 'learning_module_lesson.delete.own'),
        ('counselor', 'learning_module_lesson.reorder.own'),
        ('counselor', 'learning_module_lesson.reindex.own'),
        ('counselor', 'learning_module_quiz.view.own'),
        ('counselor', 'learning_module_quiz.upsert.own'),
        ('counselor', 'learning_module_quiz_question.create.own'),
        ('counselor', 'learning_module_quiz_question.update.own'),
        ('counselor', 'learning_module_quiz_question.delete.own'),
        ('counselor', 'learning_module_quiz_question.reorder.own'),

        -- Admin platform governance permissions
        ('admin', 'user.view.any'),
        ('admin', 'user.update.any'),
        ('admin', 'user.suspend.any'),
        ('admin', 'user.restore.any'),
        ('admin', 'user.delete.any'),
        ('admin', 'role.view.any'),
        ('admin', 'role.create.any'),
        ('admin', 'role.update.any'),
        ('admin', 'role.delete.any'),
        ('admin', 'permission.view.any'),
        ('admin', 'permission.create.any'),
        ('admin', 'permission.update.any'),
        ('admin', 'permission.delete.any'),
        ('admin', 'role_permission.view.any'),
        ('admin', 'role_permission.assign.any'),
        ('admin', 'role_permission.revoke.any'),
        ('admin', 'user_role.view.any'),
        ('admin', 'user_role.assign.any'),
        ('admin', 'user_role.revoke.any'),
        ('admin', 'system_health.view.any'),
        ('admin', 'skill.view.any'),
        ('admin', 'skill.create.any'),
        ('admin', 'skill.update.any'),
        ('admin', 'skill.delete.any')
)
INSERT INTO public.permission_role (permission_id, role_id)
SELECT p.permission_id, r.role_id
FROM role_permissions rp
JOIN public.role r
    ON r.role_name = rp.role_name
JOIN public.permission p
    ON p.permission_name = rp.permission_name
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
