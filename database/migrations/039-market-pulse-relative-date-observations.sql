-- ============================================================
-- 042-market-pulse-relative-date-observations.sql
-- Purpose:
--   Preserve the evidence range behind week/month relative dates.
-- Prerequisite:
--   041-market-pulse-post-date-confidence.sql
-- ============================================================

BEGIN;

ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS post_date_lower_bound date,
    ADD COLUMN IF NOT EXISTS post_date_upper_bound date,
    ADD COLUMN IF NOT EXISTS post_date_observed_on date;

UPDATE public.job_posting
SET post_date_lower_bound = COALESCE(post_date_lower_bound, published_at::date),
    post_date_upper_bound = COALESCE(post_date_upper_bound, published_at::date)
WHERE post_date_confidence = 'exact'
  AND published_at IS NOT NULL;

ALTER TABLE public.job_posting
    DROP CONSTRAINT IF EXISTS chk_job_posting_post_date_bounds;

ALTER TABLE public.job_posting
    ADD CONSTRAINT chk_job_posting_post_date_bounds
        CHECK (
            post_date_lower_bound IS NULL OR
            post_date_upper_bound IS NULL OR
            post_date_lower_bound <= post_date_upper_bound
        );

COMMENT ON COLUMN public.job_posting.post_date_lower_bound IS
    'Earliest date supported by crawler observations for a relative source date.';
COMMENT ON COLUMN public.job_posting.post_date_upper_bound IS
    'Latest date supported by crawler observations for a relative source date.';
COMMENT ON COLUMN public.job_posting.post_date_observed_on IS
    'Vietnam business date of the crawler observation used for the current estimate.';

COMMIT;
