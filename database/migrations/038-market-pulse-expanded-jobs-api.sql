-- ============================================================
-- 038-market-pulse-expanded-jobs-api.sql
-- Purpose:
--   Persist strict Python Jobs API contract/freshness metadata on
--   .NET import runs and source health records.
--   Record whether a .NET import applied missing-job lifecycle
--   changes and why lifecycle processing was skipped.
--   Preserve the Phase 3 typed Jobs API salary, experience,
--   date-confidence, and detail lifecycle fields in .NET storage.
-- ============================================================

BEGIN;

ALTER TABLE public.market_pulse_crawl_run
    ADD COLUMN IF NOT EXISTS source_total_count int,
    ADD COLUMN IF NOT EXISTS is_complete_sync boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS source_generated_at timestamptz,
    ADD COLUMN IF NOT EXISTS source_latest_success_at timestamptz;

ALTER TABLE public.market_pulse_crawl_run
    DROP CONSTRAINT IF EXISTS chk_market_pulse_crawl_run_source_total,
    ADD CONSTRAINT chk_market_pulse_crawl_run_source_total
        CHECK (source_total_count IS NULL OR source_total_count >= 0);

ALTER TABLE public.market_pulse_source_health
    ADD COLUMN IF NOT EXISTS source_generated_at timestamptz,
    ADD COLUMN IF NOT EXISTS source_latest_success_at timestamptz;

COMMENT ON COLUMN public.market_pulse_crawl_run.source_generated_at IS
    'Timestamp supplied by the Python Jobs API meta.generatedAt field.';
COMMENT ON COLUMN public.market_pulse_crawl_run.source_latest_success_at IS
    'Latest successful Python crawler timestamp supplied by the Jobs API.';
COMMENT ON COLUMN public.market_pulse_crawl_run.is_complete_sync IS
    'True only when the fetched count covers the Jobs API pagination total.';
COMMENT ON COLUMN public.market_pulse_source_health.source_latest_success_at IS
    'Latest known successful Python crawler timestamp, distinct from .NET import success.';

ALTER TABLE public.market_pulse_crawl_run
    ADD COLUMN IF NOT EXISTS missing_lifecycle_applied boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS lifecycle_skipped_reason text;

COMMENT ON COLUMN public.market_pulse_crawl_run.missing_lifecycle_applied IS
    'True only when the import was allowed to evaluate jobs absent from a complete, fresh source payload.';
COMMENT ON COLUMN public.market_pulse_crawl_run.lifecycle_skipped_reason IS
    'Stable reason code explaining why absent-job lifecycle evaluation was not applied.';

ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS salary_raw varchar(160),
    ADD COLUMN IF NOT EXISTS salary_min bigint,
    ADD COLUMN IF NOT EXISTS salary_max bigint,
    ADD COLUMN IF NOT EXISTS salary_currency varchar(16),
    ADD COLUMN IF NOT EXISTS salary_is_negotiable boolean,
    ADD COLUMN IF NOT EXISTS experience_raw varchar(160),
    ADD COLUMN IF NOT EXISTS experience_min_years int,
    ADD COLUMN IF NOT EXISTS experience_max_years int,
    ADD COLUMN IF NOT EXISTS post_date_confidence varchar(20),
    ADD COLUMN IF NOT EXISTS detail_status varchar(32),
    ADD COLUMN IF NOT EXISTS detail_last_success_at timestamptz;

ALTER TABLE public.job_posting
    DROP CONSTRAINT IF EXISTS chk_job_posting_salary_range,
    DROP CONSTRAINT IF EXISTS chk_job_posting_experience_range;

ALTER TABLE public.job_posting
    ADD CONSTRAINT chk_job_posting_salary_range
        CHECK (
            (salary_min IS NULL OR salary_min >= 0) AND
            (salary_max IS NULL OR salary_max >= 0) AND
            (salary_min IS NULL OR salary_max IS NULL OR salary_min <= salary_max)
        ),
    ADD CONSTRAINT chk_job_posting_experience_range
        CHECK (
            (experience_min_years IS NULL OR experience_min_years >= 0) AND
            (experience_max_years IS NULL OR experience_max_years >= 0) AND
            (
                experience_min_years IS NULL OR
                experience_max_years IS NULL OR
                experience_min_years <= experience_max_years
            )
        );

CREATE INDEX IF NOT EXISTS ix_job_posting_salary_range
    ON public.job_posting(salary_min, salary_max);

CREATE INDEX IF NOT EXISTS ix_job_posting_experience_range
    ON public.job_posting(experience_min_years, experience_max_years);

COMMENT ON COLUMN public.job_posting.post_date_confidence IS
    'Source parser confidence for post_date; null means the source did not provide confidence.';
COMMENT ON COLUMN public.job_posting.detail_status IS
    'Latest Python crawler detail-fetch status for this job.';

COMMIT;
