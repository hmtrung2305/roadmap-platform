-- ============================================================
-- 039-market-pulse-partial-sync-lifecycle.sql
-- Purpose:
--   Record whether a .NET import applied missing-job lifecycle
--   changes and why lifecycle processing was skipped.
-- ============================================================

BEGIN;

ALTER TABLE public.market_pulse_crawl_run
    ADD COLUMN IF NOT EXISTS missing_lifecycle_applied boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS lifecycle_skipped_reason text;

COMMENT ON COLUMN public.market_pulse_crawl_run.missing_lifecycle_applied IS
    'True only when the import was allowed to evaluate jobs absent from a complete, fresh source payload.';
COMMENT ON COLUMN public.market_pulse_crawl_run.lifecycle_skipped_reason IS
    'Stable reason code explaining why absent-job lifecycle evaluation was not applied.';

COMMIT;
