BEGIN;

-- Add own-scoped roadmap draft permissions for content managers.
INSERT INTO public.permission (permission_name)
VALUES
    ('roadmap_draft.view.own'),
    ('roadmap_draft.create.own'),
    ('roadmap_draft.update.own'),
    ('roadmap_draft.delete.own')
ON CONFLICT (permission_name) DO NOTHING;

-- Remove old broad roadmap draft access from built-in roles.
DELETE FROM public.permission_role pr
USING public.role r, public.permission p
WHERE pr.role_id = r.role_id
  AND pr.permission_id = p.permission_id
  AND r.role_name IN ('learner', 'content_manager', 'reviewer', 'admin')
  AND p.permission_name IN (
    'roadmap_draft.view.any',
    'roadmap_draft.create.any',
    'roadmap_draft.update.any',
    'roadmap_draft.delete.any'
  );

-- Admin is a platform-governance role. Give roadmap author/reviewer access by
-- assigning content_manager or reviewer explicitly, not by inheriting it here.
DELETE FROM public.permission_role pr
USING public.role r, public.permission p
WHERE pr.role_id = r.role_id
  AND pr.permission_id = p.permission_id
  AND r.role_name = 'admin'
  AND p.permission_name IN (
    'roadmap_draft.view.own',
    'roadmap_draft.create.own',
    'roadmap_draft.update.own',
    'roadmap_draft.delete.own',
    'roadmap_review.submit.own',
    'roadmap_review.view.any',
    'roadmap_review.approve.any',
    'roadmap_review.reject.any'
  );

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'content_manager'
WHERE p.permission_name IN (
    'roadmap_draft.view.own',
    'roadmap_draft.create.own',
    'roadmap_draft.update.own',
    'roadmap_draft.delete.own',
    'roadmap_review.submit.own'
)
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
