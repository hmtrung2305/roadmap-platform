-- ============================================================
-- 038-market-pulse-jobs-api-freshness.sql
-- Purpose:
--   Persist strict Python Jobs API contract/freshness metadata on
--   .NET import runs and source health records.
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

COMMIT;
