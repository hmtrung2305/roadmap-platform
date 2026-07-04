DROP TABLE IF EXISTS public.skill_gap_analysis_history CASCADE;

CREATE TABLE IF NOT EXISTS public.skill_gap_analysis_history
(
    skill_gap_analysis_history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    user_id UUID NOT NULL
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    career_role_id UUID NOT NULL
        REFERENCES public.career_role(career_role_id)
        ON DELETE RESTRICT,

    roadmap_id UUID NOT NULL
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE RESTRICT,

    roadmap_version_id UUID NOT NULL
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE RESTRICT,

    career_role_name_snapshot VARCHAR(255) NOT NULL,

    roadmap_title_snapshot VARCHAR(255) NOT NULL,

    roadmap_version_title_snapshot VARCHAR(255) NOT NULL,

    author_name_snapshot VARCHAR(255) NOT NULL,

    matched_skills INT NOT NULL DEFAULT 0,

    total_skills INT NOT NULL DEFAULT 0,

    missing_skills INT NOT NULL DEFAULT 0,

    snapshot_json JSONB NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,

    deleted_at TIMESTAMPTZ NULL
);