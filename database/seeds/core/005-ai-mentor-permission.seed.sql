BEGIN;

INSERT INTO public.permission (permission_name)
VALUES ('ai_mentor_chat.use.self')
ON CONFLICT (permission_name) DO NOTHING;

INSERT INTO public.permission_role (permission_id, role_id)
SELECT
    p.permission_id,
    r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'learner'
WHERE p.permission_name = 'ai_mentor_chat.use.self'
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;