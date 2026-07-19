BEGIN;

-- =====================================================
-- CONTENT MANAGER CATALOG PERMISSIONS
-- =====================================================

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'content_manager'
WHERE p.permission_name = 'career_role.view.catalog'
ON CONFLICT (permission_id, role_id) DO NOTHING;

-- =====================================================
-- ADMIN MARKET PULSE PERMISSIONS
-- =====================================================

INSERT INTO public.permission (permission_name)
VALUES
    ('market_pulse.manage.any'),
    ('market_pulse.view.catalog')
ON CONFLICT (permission_name) DO NOTHING;

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'admin'
WHERE p.permission_name IN (
    'market_pulse.manage.any',
    'market_pulse.view.catalog')
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
