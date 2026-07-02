BEGIN;

CREATE TABLE IF NOT EXISTS public.roadmap_version_review_event
(
    roadmap_version_review_event_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_version_id uuid NOT NULL,
    actor_user_id uuid,
    event_type varchar(30) NOT NULL,
    message text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_review_event_version
        FOREIGN KEY (roadmap_version_id)
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_review_event_actor_user
        FOREIGN KEY (actor_user_id)
        REFERENCES public."user"(user_id)
        ON DELETE SET NULL,

    CONSTRAINT chk_roadmap_review_event_type
        CHECK (event_type IN ('submitted', 'approved', 'rejected')),

    CONSTRAINT chk_roadmap_review_event_message
        CHECK (length(btrim(message)) > 0)
);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_review_event_version_id
    ON public.roadmap_version_review_event(roadmap_version_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_review_event_actor_user_id
    ON public.roadmap_version_review_event(actor_user_id);

INSERT INTO public.permission (permission_name)
VALUES ('roadmap_enrollment.migrate.self')
ON CONFLICT (permission_name) DO NOTHING;

INSERT INTO public.permission_role (permission_id, role_id)
SELECT p.permission_id, r.role_id
FROM public.permission p
JOIN public.role r
    ON r.role_name = 'learner'
WHERE p.permission_name = 'roadmap_enrollment.migrate.self'
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
