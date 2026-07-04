CREATE TABLE IF NOT EXISTS public.skill_gap_category_config
(
    skill_gap_category_config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    roadmap_id UUID NOT NULL
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE CASCADE,

    roadmap_version_id UUID NOT NULL
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE CASCADE,

    category_name VARCHAR(100) NOT NULL,

    display_order INT NOT NULL DEFAULT 0,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_skill_gap_category
        UNIQUE
        (
            roadmap_id,
            category_name
        )
);