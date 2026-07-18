-- ============================================================
-- 039-market-pulse-relative-date-observations.sql
-- Purpose:
--   Preserve the evidence range behind week/month relative dates.
-- Prerequisite:
--   038-market-pulse-expanded-jobs-api.sql
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

-- ============================================================
-- 039-market-pulse-post-date-confidence-integrity.sql
-- Purpose:
--   Repair legacy rows created while post_date_confidence was
--   nullable and align PostgreSQL with the required EF model.
-- Prerequisite:
--   038-market-pulse-expanded-jobs-api.sql
-- ============================================================

BEGIN;

-- ADD COLUMN keeps this repair safe for databases whose migration
-- history is incomplete. Existing values are preserved.
ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS post_date_confidence varchar(20);

UPDATE public.job_posting
SET post_date_confidence = CASE
    WHEN lower(trim(COALESCE(post_date_confidence, ''))) IN (
        'exact',
        'relative',
        'unknown'
    )
    THEN lower(trim(post_date_confidence))
    ELSE 'unknown'
END;

ALTER TABLE public.job_posting
    ALTER COLUMN post_date_confidence SET DEFAULT 'unknown',
    ALTER COLUMN post_date_confidence SET NOT NULL;

ALTER TABLE public.job_posting
    DROP CONSTRAINT IF EXISTS chk_job_posting_post_date_confidence;

ALTER TABLE public.job_posting
    ADD CONSTRAINT chk_job_posting_post_date_confidence
        CHECK (
            post_date_confidence IN ('exact', 'relative', 'unknown')
        );

COMMENT ON COLUMN public.job_posting.post_date_confidence IS
    'Posting date confidence: exact, relative, or unknown. Unknown dates are excluded from today metrics.';

COMMIT;
