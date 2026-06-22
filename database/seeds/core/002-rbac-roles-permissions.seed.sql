BEGIN;

-- =====================================================
-- NEW PERMISSIONS
-- =====================================================

INSERT INTO public.permission (permission_name)
VALUES
    ('skill_gap_analysis_history.view.self'),
    ('skill_gap_analysis_history.delete.self'),
    ('skill_gap_config.view.any'),
    ('skill_gap_config.update.any')
ON CONFLICT (permission_name) DO NOTHING;

-- =====================================================
-- LEARNER PERMISSIONS
-- =====================================================

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'learner'
WHERE p.permission_name IN
(
    'skill_gap_analysis_history.view.self',
    'skill_gap_analysis_history.delete.self'
)
ON CONFLICT (permission_id, role_id) DO NOTHING;

-- =====================================================
-- CONTENT MANAGER PERMISSIONS
-- =====================================================

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'content_manager'
WHERE p.permission_name IN
(
    'skill_gap_config.view.any',
    'skill_gap_config.update.any'
)
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
