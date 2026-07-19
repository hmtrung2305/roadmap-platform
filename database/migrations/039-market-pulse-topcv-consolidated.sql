-- ============================================================
-- 039-market-pulse-topcv-consolidated.sql
-- TopCV-only Market Pulse storage and publication-date analytics.
--
-- This file intentionally supersedes every earlier variant of 039.
-- Databases that ran the old 039 must run this file once explicitly.
-- It is transactional and idempotent; a non-TopCV posting aborts the
-- transaction before any destructive schema change is made.
-- ============================================================

BEGIN;

DO $$
DECLARE
    unsupported_sources text;
    duplicate_external_ids bigint;
BEGIN
    IF to_regclass('public.job_portal_source') IS NOT NULL AND
       EXISTS (
           SELECT 1
           FROM information_schema.columns
           WHERE table_schema = 'public'
             AND table_name = 'job_posting'
             AND column_name = 'job_portal_source_id'
       ) THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source.name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.job_portal_source source
        WHERE COALESCE(lower(trim(source.name)), '') <> 'topcv';

        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION
                'Migration 039 refused: non-TopCV job sources exist: %',
                unsupported_sources;
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM public.job_posting WHERE trim(external_id) = '') THEN
        RAISE EXCEPTION
            'Migration 039 refused: blank job external IDs provide no TopCV identity evidence.';
    END IF;

    SELECT string_agg(DISTINCT split_part(external_id, ':', 1), ', ')
    INTO unsupported_sources
    FROM public.job_posting
    WHERE external_id ~ '^[[:alnum:]_-]+:'
      AND lower(split_part(external_id, ':', 1)) <> 'topcv';
    IF unsupported_sources IS NOT NULL THEN
        RAISE EXCEPTION
            'Migration 039 refused: job external IDs contain non-TopCV prefixes: %',
            unsupported_sources;
    END IF;

    IF to_regclass('public.market_pulse_crawl_run') IS NOT NULL AND EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_crawl_run'
          AND column_name = 'source_name'
    ) THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source_name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.market_pulse_crawl_run
        WHERE COALESCE(lower(trim(source_name)), '') <> 'topcv';
        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION 'Migration 039 refused: non-TopCV import-run sources exist: %', unsupported_sources;
        END IF;
    ELSIF to_regclass('public.market_pulse_import_run') IS NOT NULL AND EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_import_run'
          AND column_name = 'source_name'
    ) THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source_name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.market_pulse_import_run
        WHERE COALESCE(lower(trim(source_name)), '') <> 'topcv';
        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION 'Migration 039 refused: non-TopCV import-run sources exist: %', unsupported_sources;
        END IF;
    END IF;

    IF to_regclass('public.market_pulse_failed_item') IS NOT NULL AND EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_failed_item'
          AND column_name = 'source_name'
    ) THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source_name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.market_pulse_failed_item
        WHERE COALESCE(lower(trim(source_name)), '') <> 'topcv';
        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION 'Migration 039 refused: non-TopCV import-failure sources exist: %', unsupported_sources;
        END IF;
    ELSIF to_regclass('public.market_pulse_import_failure') IS NOT NULL AND EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_import_failure'
          AND column_name = 'source_name'
    ) THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source_name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.market_pulse_import_failure
        WHERE COALESCE(lower(trim(source_name)), '') <> 'topcv';
        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION 'Migration 039 refused: non-TopCV import-failure sources exist: %', unsupported_sources;
        END IF;
    END IF;

    IF to_regclass('public.market_pulse_source_health') IS NOT NULL THEN
        SELECT string_agg(
            DISTINCT COALESCE(NULLIF(trim(source_name), ''), '<null-or-blank>'),
            ', ')
        INTO unsupported_sources
        FROM public.market_pulse_source_health
        WHERE COALESCE(lower(trim(source_name)), '') <> 'topcv';
        IF unsupported_sources IS NOT NULL THEN
            RAISE EXCEPTION 'Migration 039 refused: non-TopCV source-health rows exist: %', unsupported_sources;
        END IF;
    END IF;

    SELECT count(*) INTO duplicate_external_ids
    FROM (
        SELECT external_id
        FROM public.job_posting
        GROUP BY external_id
        HAVING count(*) > 1
    ) duplicates;
    IF duplicate_external_ids > 0 THEN
        RAISE EXCEPTION
            'Migration 039 refused: % duplicate external_id values prevent global uniqueness.',
            duplicate_external_ids;
    END IF;
END $$;

-- Normalize posting-date evidence before analytics reads it.
ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS post_date_confidence varchar(20),
    ADD COLUMN IF NOT EXISTS post_date_lower_bound date,
    ADD COLUMN IF NOT EXISTS post_date_upper_bound date,
    ADD COLUMN IF NOT EXISTS post_date_observed_on date;

UPDATE public.job_posting
SET post_date_confidence = CASE
    WHEN lower(trim(COALESCE(post_date_confidence, ''))) IN ('exact', 'relative', 'unknown')
        THEN lower(trim(post_date_confidence))
    ELSE 'unknown'
END;

UPDATE public.job_posting
SET post_date_lower_bound = COALESCE(
        post_date_lower_bound,
        (published_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date),
    post_date_upper_bound = COALESCE(
        post_date_upper_bound,
        (published_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date)
WHERE post_date_confidence = 'exact'
  AND published_at IS NOT NULL;

ALTER TABLE public.job_posting
    ALTER COLUMN post_date_confidence SET DEFAULT 'unknown',
    ALTER COLUMN post_date_confidence SET NOT NULL,
    DROP CONSTRAINT IF EXISTS chk_job_posting_post_date_confidence,
    DROP CONSTRAINT IF EXISTS chk_job_posting_post_date_bounds;

ALTER TABLE public.job_posting
    ADD CONSTRAINT chk_job_posting_post_date_confidence
        CHECK (post_date_confidence IN ('exact', 'relative', 'unknown')),
    ADD CONSTRAINT chk_job_posting_post_date_bounds
        CHECK (
            post_date_lower_bound IS NULL OR
            post_date_upper_bound IS NULL OR
            post_date_lower_bound <= post_date_upper_bound
        );

-- Rename import operational tables and their key columns without changing UUIDs.
DO $$
BEGIN
    IF to_regclass('public.market_pulse_import_run') IS NULL AND
       to_regclass('public.market_pulse_crawl_run') IS NOT NULL THEN
        ALTER TABLE public.market_pulse_crawl_run RENAME TO market_pulse_import_run;
    END IF;
    IF to_regclass('public.market_pulse_import_failure') IS NULL AND
       to_regclass('public.market_pulse_failed_item') IS NOT NULL THEN
        ALTER TABLE public.market_pulse_failed_item RENAME TO market_pulse_import_failure;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_import_run'
          AND column_name = 'market_pulse_crawl_run_id'
    ) THEN
        ALTER TABLE public.market_pulse_import_run
            RENAME COLUMN market_pulse_crawl_run_id TO market_pulse_import_run_id;
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_import_failure'
          AND column_name = 'market_pulse_failed_item_id'
    ) THEN
        ALTER TABLE public.market_pulse_import_failure
            RENAME COLUMN market_pulse_failed_item_id TO market_pulse_import_failure_id;
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'market_pulse_import_failure'
          AND column_name = 'market_pulse_crawl_run_id'
    ) THEN
        ALTER TABLE public.market_pulse_import_failure
            RENAME COLUMN market_pulse_crawl_run_id TO market_pulse_import_run_id;
    END IF;
END $$;

-- Remove duplicate crawler-observation analytics. Publication analytics reads canonical jobs.
DROP TABLE IF EXISTS public.market_pulse_daily_posting_observation CASCADE;
DROP TABLE IF EXISTS public.market_pulse_daily_observation CASCADE;
DROP TABLE IF EXISTS public.market_pulse_source_health CASCADE;

-- TopCV provenance is implied, not repeated in every row.
ALTER TABLE public.job_posting DROP CONSTRAINT IF EXISTS fk_job_posting_source;
ALTER TABLE public.job_posting DROP CONSTRAINT IF EXISTS uq_job_posting_source_external;
DROP INDEX IF EXISTS public.uq_job_posting_source_external;
DROP INDEX IF EXISTS public.ix_job_posting_source;
ALTER TABLE public.job_posting DROP COLUMN IF EXISTS job_portal_source_id;
DROP TABLE IF EXISTS public.job_portal_source CASCADE;

ALTER TABLE public.market_pulse_import_run DROP COLUMN IF EXISTS source_name;
ALTER TABLE public.market_pulse_import_failure DROP COLUMN IF EXISTS source_name;

DROP INDEX IF EXISTS public.ix_market_pulse_crawl_run_source;
DROP INDEX IF EXISTS public.ix_market_pulse_failed_item_source_stage;
DROP INDEX IF EXISTS public.ix_market_pulse_crawl_run_started_status;
DROP INDEX IF EXISTS public.ix_market_pulse_failed_item_status_created;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'public.market_pulse_import_run'::regclass
          AND conname = 'market_pulse_crawl_run_pkey'
    ) THEN
        ALTER TABLE public.market_pulse_import_run
            RENAME CONSTRAINT market_pulse_crawl_run_pkey TO market_pulse_import_run_pkey;
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'public.market_pulse_import_failure'::regclass
          AND conname = 'market_pulse_failed_item_pkey'
    ) THEN
        ALTER TABLE public.market_pulse_import_failure
            RENAME CONSTRAINT market_pulse_failed_item_pkey TO market_pulse_import_failure_pkey;
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'public.market_pulse_import_failure'::regclass
          AND conname = 'fk_market_pulse_failed_item_run'
    ) THEN
        ALTER TABLE public.market_pulse_import_failure
            RENAME CONSTRAINT fk_market_pulse_failed_item_run TO fk_market_pulse_import_failure_run;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS uq_job_posting_external_id
    ON public.job_posting(external_id);
CREATE INDEX IF NOT EXISTS ix_job_posting_publication_bounds
    ON public.job_posting(post_date_lower_bound, post_date_upper_bound)
    WHERE post_date_confidence <> 'unknown';
CREATE INDEX IF NOT EXISTS ix_job_posting_publication_filters
    ON public.job_posting(category, location, lifecycle_status);
CREATE INDEX IF NOT EXISTS ix_market_pulse_import_run_started_status
    ON public.market_pulse_import_run(started_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_market_pulse_import_failure_status_created
    ON public.market_pulse_import_failure(status, created_at DESC);

CREATE TABLE IF NOT EXISTS public.market_pulse_publication_history_state
(
    singleton_id smallint PRIMARY KEY DEFAULT 1,
    coverage_start date NOT NULL,
    coverage_end date NOT NULL,
    source_data_at timestamptz NOT NULL,
    last_successful_sync_at timestamptz NOT NULL,
    synced_posting_count int NOT NULL DEFAULT 0,
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT chk_market_pulse_publication_history_singleton CHECK (singleton_id = 1),
    CONSTRAINT chk_market_pulse_publication_history_dates CHECK (coverage_start <= coverage_end),
    CONSTRAINT chk_market_pulse_publication_history_count CHECK (synced_posting_count >= 0)
);

CREATE TABLE IF NOT EXISTS public.market_pulse_refresh_operation
(
    market_pulse_refresh_operation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    status varchar(24) NOT NULL DEFAULT 'queued',
    baseline_crawler_success_at timestamptz NOT NULL,
    crawler_success_at timestamptz,
    market_pulse_import_run_id uuid,
    current_step varchar(24) NOT NULL DEFAULT 'crawler',
    trigger_type varchar(24) NOT NULL DEFAULT 'manual',
    error_code varchar(80),
    error_message text,
    requested_at timestamptz NOT NULL DEFAULT now(),
    started_at timestamptz,
    finished_at timestamptz,
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_market_pulse_refresh_operation_import_run
        FOREIGN KEY (market_pulse_import_run_id)
        REFERENCES public.market_pulse_import_run(market_pulse_import_run_id)
        ON DELETE SET NULL,
    CONSTRAINT chk_market_pulse_refresh_operation_status
        CHECK (status IN ('queued', 'crawling', 'importing', 'success', 'failed')),
    CONSTRAINT chk_market_pulse_refresh_operation_step
        CHECK (current_step IN ('crawler', 'import', 'analytics'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_market_pulse_refresh_operation_active
    ON public.market_pulse_refresh_operation ((1))
    WHERE status IN ('queued', 'crawling', 'importing');
CREATE INDEX IF NOT EXISTS ix_market_pulse_refresh_operation_requested_at
    ON public.market_pulse_refresh_operation(requested_at DESC);

COMMENT ON TABLE public.market_pulse_publication_history_state IS
    'Singleton initialized by complete scope=all TopCV history sync; coverage end may extend only through contiguous complete imports.';
COMMENT ON TABLE public.market_pulse_refresh_operation IS
    'Durable TopCV crawler -> .NET import -> analytics-ready operation state.';
COMMENT ON COLUMN public.job_posting.post_date_confidence IS
    'Publication-date evidence: exact, relative, or unknown. Relative intervals remain estimated.';

-- Admin Operations Console links to the public Market Pulse view. Permission checks do not
-- imply manage -> view, so grant both capabilities explicitly for existing deployments.
INSERT INTO public.permission (permission_name)
VALUES ('market_pulse.view.catalog')
ON CONFLICT (permission_name) DO NOTHING;

INSERT INTO public.permission_role (permission_id, role_id)
SELECT permission.permission_id, role.role_id
FROM public.permission permission
JOIN public.role role ON role.role_name = 'admin'
WHERE permission.permission_name = 'market_pulse.view.catalog'
ON CONFLICT (permission_id, role_id) DO NOTHING;

COMMIT;
