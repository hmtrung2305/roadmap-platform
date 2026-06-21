CREATE TABLE IF NOT EXISTS public.skill_gap_analysis_history
(
    skill_gap_analysis_history_id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    career_role_id UUID NOT NULL,
    career_role_slug VARCHAR(200) NOT NULL,
    career_role_name VARCHAR(500) NOT NULL,
    readiness_percent NUMERIC(5,2) NOT NULL,
    skill_coverage_percent NUMERIC(5,2) NOT NULL,
    snapshot_json JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_skill_gap_history_user
        FOREIGN KEY(user_id)
        REFERENCES "user"(user_id),

    CONSTRAINT fk_skill_gap_history_role
        FOREIGN KEY(career_role_id)
        REFERENCES career_role(career_role_id)
);