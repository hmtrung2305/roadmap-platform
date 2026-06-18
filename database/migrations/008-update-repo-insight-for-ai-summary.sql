-- 008-update-repo-insight-for-ai-summary.sql
-- Adds lean AI summary support fields to repo_insight.
-- Assumes repo_insight has no existing insight rows.

ALTER TABLE public.repo_insight
ADD COLUMN IF NOT EXISTS analysis_status varchar(50) NOT NULL DEFAULT 'completed',
ADD COLUMN IF NOT EXISTS readme_hash varchar(64),
ADD COLUMN IF NOT EXISTS readme_truncated bool NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS ai_model varchar(100),
ADD COLUMN IF NOT EXISTS error_message text,
ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();

ALTER TABLE public.repo_insight
ALTER COLUMN tech_stack SET DEFAULT '[]'::jsonb,
ALTER COLUMN detected_skills SET DEFAULT '[]'::jsonb;

UPDATE public.repo_insight
SET tech_stack = '[]'::jsonb
WHERE tech_stack IS NULL;

UPDATE public.repo_insight
SET detected_skills = '[]'::jsonb
WHERE detected_skills IS NULL;

ALTER TABLE public.repo_insight
ALTER COLUMN tech_stack SET NOT NULL,
ALTER COLUMN detected_skills SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_repo_insight_repository_id'
    ) THEN
        ALTER TABLE public.repo_insight
        ADD CONSTRAINT uq_repo_insight_repository_id
        UNIQUE (repository_id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_repo_insight_analysis_status'
    ) THEN
        ALTER TABLE public.repo_insight
        ADD CONSTRAINT chk_repo_insight_analysis_status
        CHECK (analysis_status IN ('pending', 'completed', 'failed'));
    END IF;
END $$;