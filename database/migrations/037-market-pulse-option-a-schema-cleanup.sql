-- ============================================================
-- 037-market-pulse-option-a-schema-cleanup.sql
-- Purpose:
--   Align Market Pulse storage with Option A: Python provides job
--   data and .NET owns import, persistence, analytics, and APIs.
-- ============================================================

BEGIN;

-- Keep normalized skills with the imported posting. The overview is
-- computed directly from job_posting, so a second skill taxonomy is not
-- required for the current feature.
ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS skills jsonb NOT NULL DEFAULT '[]'::jsonb;

UPDATE public.job_posting
SET skills = '[]'::jsonb
WHERE skills IS NULL OR jsonb_typeof(skills) <> 'array';

ALTER TABLE public.job_posting
    DROP CONSTRAINT IF EXISTS chk_job_posting_skills_json_array,
    ADD CONSTRAINT chk_job_posting_skills_json_array
        CHECK (jsonb_typeof(skills) = 'array');

-- The legacy crawl_run name is retained for migration compatibility. Rows
-- now describe .NET import runs from the Python Jobs API.
ALTER TABLE public.market_pulse_crawl_run
    ADD COLUMN IF NOT EXISTS trigger_type varchar(40) NOT NULL DEFAULT 'manual',
    ADD COLUMN IF NOT EXISTS imported_count int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS updated_count int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS skipped_count int NOT NULL DEFAULT 0;

ALTER TABLE public.market_pulse_crawl_run
    ALTER COLUMN mode SET DEFAULT 'jobs_api_pull',
    DROP CONSTRAINT IF EXISTS chk_market_pulse_crawl_run_counts,
    ADD CONSTRAINT chk_market_pulse_crawl_run_counts
        CHECK (
            fetched_count >= 0 AND
            saved_count >= 0 AND
            imported_count >= 0 AND
            updated_count >= 0 AND
            skipped_count >= 0 AND
            duplicate_count >= 0 AND
            failed_count >= 0
        );

COMMENT ON TABLE public.market_pulse_crawl_run IS
    'Legacy table name retained; records .NET import runs from the Python Jobs API.';
COMMENT ON TABLE public.market_pulse_failed_item IS
    'Items that failed during .NET import; does not represent Python crawler retries.';

-- Normalize the historical provider label only when doing so cannot collide
-- with an existing logical TopCV source.
UPDATE public.job_portal_source
SET name = 'topcv', updated_at = now()
WHERE lower(name) = 'jobs api'
  AND NOT EXISTS (
      SELECT 1
      FROM public.job_portal_source existing
      WHERE lower(existing.name) = 'topcv'
  );

-- These tables were write-only or fully redundant. No API, DTO, controller,
-- frontend component, or retained service path reads them after this migration.
DROP TABLE IF EXISTS public.job_skill_mention;
DROP TABLE IF EXISTS public.skill_taxonomy;
DROP TABLE IF EXISTS public.job_posting_observation;
DROP TABLE IF EXISTS public.job_posting_version;
DROP TABLE IF EXISTS public.job_posting_daily_snapshot;
DROP TABLE IF EXISTS public.skill_trend_snapshot;
DROP TABLE IF EXISTS public.market_pulse_insight_snapshot;
DROP TABLE IF EXISTS public.job_market_daily_snapshot;

COMMIT;
