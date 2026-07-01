-- =====================================================================
-- RBAC Core Seed: Roles, Permissions, and Role-Permission Assignments
-- =====================================================================
-- Purpose:
--   Seeds the core RBAC data used by the Roadmap Platform.
--
-- Model:
--   - learner   = learning participant
--   - content_manager = learning content operator
--   - reviewer = content review operator
--   - admin     = platform operator
--
-- Important product rule:
--   Roles are separate product personas. Content Manager and admin do not
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
    ('content_manager'),
    ('reviewer'),
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
    ('roadmap_enrollment.migrate.self'),
    ('roadmap_progress.update.self'),

    -- Roadmap draft and review workflow permissions
    ('roadmap_draft.view.own'),
    ('roadmap_draft.create.own'),
    ('roadmap_draft.update.own'),
    ('roadmap_draft.delete.own'),
    ('roadmap_review.submit.own'),
    ('roadmap_review.view.any'),
    ('roadmap_review.approve.any'),
    ('roadmap_review.reject.any'),

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
    ('skill.create.catalog'),
    ('skill.update.catalog'),
    ('learning_resource.view.catalog'),
    ('learning_resource.create.catalog'),
    ('learning_resource.update.catalog'),

    -- Admin skill governance permissions
    ('skill.view.any'),

    -- Content Manager learning module ownership permissions
    ('learning_module.view.own'),
    ('learning_module.create.own'),
    ('learning_module.update.own'),
    ('learning_module.delete.own'),
    ('learning_module.publish.own'),
    ('learning_module.archive.own'),
    ('learning_module.preview.own'),

    -- Content Manager lesson ownership permissions
    ('learning_module_lesson.create.own'),
    ('learning_module_lesson.update.own'),
    ('learning_module_lesson.delete.own'),
    ('learning_module_lesson.reorder.own'),
    ('learning_module_lesson.reindex.own'),

    -- Content Manager quiz ownership permissions
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
-- This seed owns the baseline permissions for the built-in roles below.
-- Re-running it should converge the database back to this canonical v1
-- mapping, including removing outdated mappings such as content_manager ->
-- skill.view.any from earlier draft seeds.
WITH managed_roles(role_name) AS (
    VALUES
        ('learner'),
        ('content_manager'),
        ('reviewer'),
        ('admin')
),
managed_permissions(permission_name) AS (
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
        ('roadmap_enrollment.migrate.self'),
        ('roadmap_progress.update.self'),

        -- Roadmap draft and review workflow permissions
        ('roadmap_draft.view.own'),
        ('roadmap_draft.create.own'),
        ('roadmap_draft.update.own'),
        ('roadmap_draft.delete.own'),
        ('roadmap_review.submit.own'),
        ('roadmap_review.view.any'),
        ('roadmap_review.approve.any'),
        ('roadmap_review.reject.any'),

        -- Legacy broad roadmap draft permissions removed from built-in roles
        ('roadmap_draft.view.any'),
        ('roadmap_draft.create.any'),
        ('roadmap_draft.update.any'),
        ('roadmap_draft.delete.any'),

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

        -- Catalog/discovery permissions
        ('career_role.view.catalog'),
        ('skill_gap_analysis.create.self'),
        ('market_pulse.view.catalog'),
        ('skill.view.catalog'),
        ('skill.create.catalog'),
        ('skill.update.catalog'),
        ('learning_resource.view.catalog'),
        ('learning_resource.create.catalog'),
        ('learning_resource.update.catalog'),

        -- Content Manager learning module ownership permissions
        ('learning_module.view.own'),
        ('learning_module.create.own'),
        ('learning_module.update.own'),
        ('learning_module.delete.own'),
        ('learning_module.publish.own'),
        ('learning_module.archive.own'),
        ('learning_module.preview.own'),
        ('learning_module_lesson.create.own'),
        ('learning_module_lesson.update.own'),
        ('learning_module_lesson.delete.own'),
        ('learning_module_lesson.reorder.own'),
        ('learning_module_lesson.reindex.own'),
        ('learning_module_quiz.view.own'),
        ('learning_module_quiz.upsert.own'),
        ('learning_module_quiz_question.create.own'),
        ('learning_module_quiz_question.update.own'),
        ('learning_module_quiz_question.delete.own'),
        ('learning_module_quiz_question.reorder.own'),

        -- Admin platform governance permissions
        ('user.view.any'),
        ('user.update.any'),
        ('user.suspend.any'),
        ('user.restore.any'),
        ('user.delete.any'),
        ('role.view.any'),
        ('role.create.any'),
        ('role.update.any'),
        ('role.delete.any'),
        ('permission.view.any'),
        ('permission.create.any'),
        ('permission.update.any'),
        ('permission.delete.any'),
        ('role_permission.view.any'),
        ('role_permission.assign.any'),
        ('role_permission.revoke.any'),
        ('user_role.view.any'),
        ('user_role.assign.any'),
        ('user_role.revoke.any'),
        ('system_health.view.any'),
        ('skill.view.any'),
        ('skill.create.any'),
        ('skill.update.any'),
        ('skill.delete.any')
)
DELETE FROM public.permission_role pr
USING public.role r, public.permission p, managed_roles mr, managed_permissions mp
WHERE pr.role_id = r.role_id
  AND pr.permission_id = p.permission_id
  AND r.role_name = mr.role_name
  AND p.permission_name = mp.permission_name;

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

        ('content_manager', 'account.view.self'),
        ('content_manager', 'account.update.self'),
        ('content_manager', 'auth_provider.view.self'),
        ('content_manager', 'auth_provider.link.self'),
        ('content_manager', 'auth_provider.unlink.self'),
        ('content_manager', 'auth_provider.update.self'),
        ('content_manager', 'profile.view.self'),
        ('content_manager', 'profile.update.self'),

        ('reviewer', 'account.view.self'),
        ('reviewer', 'account.update.self'),
        ('reviewer', 'auth_provider.view.self'),
        ('reviewer', 'auth_provider.link.self'),
        ('reviewer', 'auth_provider.unlink.self'),
        ('reviewer', 'auth_provider.update.self'),
        ('reviewer', 'profile.view.self'),
        ('reviewer', 'profile.update.self'),

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
        ('learner', 'roadmap_enrollment.migrate.self'),
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

        -- Content Manager catalog lookup and content management permissions
        ('content_manager', 'skill.view.catalog'),
        ('content_manager', 'skill.create.catalog'),
        ('content_manager', 'skill.update.catalog'),
        ('content_manager', 'learning_resource.view.catalog'),
        ('content_manager', 'learning_resource.create.catalog'),
        ('content_manager', 'learning_resource.update.catalog'),
        ('content_manager', 'roadmap_draft.view.own'),
        ('content_manager', 'roadmap_draft.create.own'),
        ('content_manager', 'roadmap_draft.update.own'),
        ('content_manager', 'roadmap_draft.delete.own'),
        ('content_manager', 'roadmap_review.submit.own'),
        ('content_manager', 'learning_module.view.own'),
        ('content_manager', 'learning_module.create.own'),
        ('content_manager', 'learning_module.update.own'),
        ('content_manager', 'learning_module.delete.own'),
        ('content_manager', 'learning_module.publish.own'),
        ('content_manager', 'learning_module.archive.own'),
        ('content_manager', 'learning_module.preview.own'),
        ('content_manager', 'learning_module_lesson.create.own'),
        ('content_manager', 'learning_module_lesson.update.own'),
        ('content_manager', 'learning_module_lesson.delete.own'),
        ('content_manager', 'learning_module_lesson.reorder.own'),
        ('content_manager', 'learning_module_lesson.reindex.own'),
        ('content_manager', 'learning_module_quiz.view.own'),
        ('content_manager', 'learning_module_quiz.upsert.own'),
        ('content_manager', 'learning_module_quiz_question.create.own'),
        ('content_manager', 'learning_module_quiz_question.update.own'),
        ('content_manager', 'learning_module_quiz_question.delete.own'),
        ('content_manager', 'learning_module_quiz_question.reorder.own'),

        -- Reviewer content approval permissions
        ('reviewer', 'roadmap_review.view.any'),
        ('reviewer', 'roadmap_review.approve.any'),
        ('reviewer', 'roadmap_review.reject.any'),

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
        ('admin', 'skill.view.catalog'),
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
