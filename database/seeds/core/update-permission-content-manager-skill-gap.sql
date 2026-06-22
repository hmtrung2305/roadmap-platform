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


