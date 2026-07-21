-- Consolidate Market Pulse operational state and remove the unused payment schema.
-- This migration preserves import, refresh, history-watermark and failure rows.

BEGIN;

SELECT pg_advisory_xact_lock(6418325908117204774);

CREATE TABLE IF NOT EXISTS public.market_pulse_pipeline_run
(
    market_pulse_pipeline_run_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_type varchar(24) NOT NULL,
    status varchar(40) NOT NULL DEFAULT 'running',
    mode varchar(40) NOT NULL DEFAULT 'jobs_api_pull',
    trigger_type varchar(40) NOT NULL DEFAULT 'manual',
    current_step varchar(24) NOT NULL DEFAULT 'import',
    baseline_crawler_success_at timestamptz NOT NULL DEFAULT '1970-01-01 00:00:00+00',
    crawler_success_at timestamptz,
    import_run_id uuid,
    requested_at timestamptz NOT NULL DEFAULT now(),
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    duration_ms int,
    fetched_count int NOT NULL DEFAULT 0,
    source_total_count int,
    is_complete_sync boolean NOT NULL DEFAULT false,
    missing_lifecycle_applied boolean NOT NULL DEFAULT false,
    lifecycle_skipped_reason text,
    source_generated_at timestamptz,
    source_latest_success_at timestamptz,
    saved_count int NOT NULL DEFAULT 0,
    imported_count int NOT NULL DEFAULT 0,
    updated_count int NOT NULL DEFAULT 0,
    skipped_count int NOT NULL DEFAULT 0,
    duplicate_count int NOT NULL DEFAULT 0,
    failed_count int NOT NULL DEFAULT 0,
    stopped_reason text,
    error_summary text,
    error_code varchar(80),
    error_message text,
    coverage_start date,
    coverage_end date,
    source_data_at timestamptz,
    last_successful_sync_at timestamptz,
    synced_posting_count int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_market_pulse_pipeline_operation_type
        CHECK (operation_type IN ('import', 'refresh', 'history_sync')),
    CONSTRAINT chk_market_pulse_pipeline_counts
        CHECK (
            fetched_count >= 0 AND saved_count >= 0 AND imported_count >= 0 AND
            updated_count >= 0 AND skipped_count >= 0 AND duplicate_count >= 0 AND
            failed_count >= 0 AND synced_posting_count >= 0
        ),
    CONSTRAINT chk_market_pulse_pipeline_source_total
        CHECK (source_total_count IS NULL OR source_total_count >= 0),
    CONSTRAINT chk_market_pulse_pipeline_history_dates
        CHECK (coverage_start IS NULL OR coverage_end IS NULL OR coverage_start <= coverage_end),
    CONSTRAINT chk_market_pulse_pipeline_refresh_status
        CHECK (
            operation_type <> 'refresh' OR
            status IN ('queued', 'crawling', 'importing', 'success', 'failed')
        ),
    CONSTRAINT chk_market_pulse_pipeline_step
        CHECK (current_step IN ('crawler', 'import', 'analytics'))
);

-- Prevent application writes from racing the copy-and-verify sequence. On a
-- second run only the consolidated tables remain, so missing legacy tables are skipped.
DO $migration$
BEGIN
    IF to_regclass('public.market_pulse_import_run') IS NOT NULL THEN
        EXECUTE 'LOCK TABLE public.market_pulse_import_run IN ACCESS EXCLUSIVE MODE';
    END IF;
    IF to_regclass('public.market_pulse_refresh_operation') IS NOT NULL THEN
        EXECUTE 'LOCK TABLE public.market_pulse_refresh_operation IN ACCESS EXCLUSIVE MODE';
    END IF;
    IF to_regclass('public.market_pulse_publication_history_state') IS NOT NULL THEN
        EXECUTE 'LOCK TABLE public.market_pulse_publication_history_state IN ACCESS EXCLUSIVE MODE';
    END IF;
    IF to_regclass('public.market_pulse_import_failure') IS NOT NULL THEN
        EXECUTE 'LOCK TABLE public.market_pulse_import_failure IN ACCESS EXCLUSIVE MODE';
    END IF;
END
$migration$;

DO $migration$
DECLARE
    collision_count bigint;
BEGIN
    IF to_regclass('public.market_pulse_import_run') IS NOT NULL AND
       to_regclass('public.market_pulse_refresh_operation') IS NOT NULL THEN
        EXECUTE $sql$
            SELECT count(*)
            FROM public.market_pulse_import_run import_run
            JOIN public.market_pulse_refresh_operation refresh
              ON refresh.market_pulse_refresh_operation_id = import_run.market_pulse_import_run_id
        $sql$ INTO collision_count;

        IF collision_count <> 0 THEN
            RAISE EXCEPTION
                'Cannot consolidate Market Pulse: % import/refresh UUID collisions found.',
                collision_count;
        END IF;
    END IF;
END
$migration$;

DO $migration$
BEGIN
    IF to_regclass('public.market_pulse_import_run') IS NOT NULL THEN
        EXECUTE $sql$
            INSERT INTO public.market_pulse_pipeline_run
            (
                market_pulse_pipeline_run_id, operation_type, status, mode, trigger_type,
                current_step, requested_at, started_at, finished_at, duration_ms,
                fetched_count, source_total_count, is_complete_sync,
                missing_lifecycle_applied, lifecycle_skipped_reason, source_generated_at,
                source_latest_success_at, saved_count, imported_count, updated_count,
                skipped_count, duplicate_count, failed_count, stopped_reason,
                error_summary, created_at, updated_at
            )
            SELECT
                market_pulse_import_run_id, 'import', status, mode, trigger_type,
                'import', started_at, started_at, finished_at, duration_ms,
                fetched_count, source_total_count, is_complete_sync,
                missing_lifecycle_applied, lifecycle_skipped_reason, source_generated_at,
                source_latest_success_at, saved_count, imported_count, updated_count,
                skipped_count, duplicate_count, failed_count, stopped_reason,
                error_summary, created_at, COALESCE(finished_at, created_at)
            FROM public.market_pulse_import_run
            ON CONFLICT (market_pulse_pipeline_run_id) DO NOTHING
        $sql$;

        IF (SELECT count(*) FROM public.market_pulse_pipeline_run WHERE operation_type = 'import') <>
           (SELECT count(*) FROM public.market_pulse_import_run) THEN
            RAISE EXCEPTION 'Market Pulse import-run row-count verification failed.';
        END IF;
    END IF;
END
$migration$;

DO $migration$
BEGIN
    IF to_regclass('public.market_pulse_refresh_operation') IS NOT NULL THEN
        EXECUTE $sql$
            INSERT INTO public.market_pulse_pipeline_run
            (
                market_pulse_pipeline_run_id, operation_type, status, mode, trigger_type,
                current_step, baseline_crawler_success_at, crawler_success_at,
                import_run_id, requested_at, started_at, finished_at, error_code,
                error_message, created_at, updated_at
            )
            SELECT
                market_pulse_refresh_operation_id, 'refresh', status, 'end_to_end',
                trigger_type, current_step, baseline_crawler_success_at,
                crawler_success_at, market_pulse_import_run_id, requested_at,
                COALESCE(started_at, requested_at), finished_at, error_code,
                error_message, requested_at, updated_at
            FROM public.market_pulse_refresh_operation
            ON CONFLICT (market_pulse_pipeline_run_id) DO NOTHING
        $sql$;

        IF (SELECT count(*) FROM public.market_pulse_pipeline_run WHERE operation_type = 'refresh') <>
           (SELECT count(*) FROM public.market_pulse_refresh_operation) THEN
            RAISE EXCEPTION 'Market Pulse refresh-operation row-count verification failed.';
        END IF;
    END IF;
END
$migration$;

DO $migration$
BEGIN
    IF to_regclass('public.market_pulse_publication_history_state') IS NOT NULL THEN
        EXECUTE $sql$
            INSERT INTO public.market_pulse_pipeline_run
            (
                market_pulse_pipeline_run_id, operation_type, status, mode, trigger_type,
                current_step, requested_at, started_at, finished_at, coverage_start,
                coverage_end, source_data_at, last_successful_sync_at,
                synced_posting_count, created_at, updated_at
            )
            SELECT
                gen_random_uuid(), 'history_sync', 'success', 'history_sync',
                'history_sync', 'analytics', last_successful_sync_at,
                last_successful_sync_at, last_successful_sync_at, coverage_start,
                coverage_end, source_data_at, last_successful_sync_at,
                synced_posting_count, last_successful_sync_at, updated_at
            FROM public.market_pulse_publication_history_state
            WHERE NOT EXISTS
            (
                SELECT 1 FROM public.market_pulse_pipeline_run
                WHERE operation_type = 'history_sync'
            )
        $sql$;

        IF (SELECT count(*) FROM public.market_pulse_pipeline_run WHERE operation_type = 'history_sync') <>
           (SELECT count(*) FROM public.market_pulse_publication_history_state) THEN
            RAISE EXCEPTION 'Market Pulse history-state row-count verification failed.';
        END IF;
    END IF;
END
$migration$;

ALTER TABLE public.market_pulse_pipeline_run
    DROP CONSTRAINT IF EXISTS fk_market_pulse_pipeline_import_run;

ALTER TABLE public.market_pulse_pipeline_run
    ADD CONSTRAINT fk_market_pulse_pipeline_import_run
        FOREIGN KEY (import_run_id)
        REFERENCES public.market_pulse_pipeline_run(market_pulse_pipeline_run_id)
        ON DELETE SET NULL;

ALTER TABLE public.market_pulse_import_failure
    ADD COLUMN IF NOT EXISTS market_pulse_pipeline_run_id uuid;

DO $migration$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'market_pulse_import_failure'
          AND column_name = 'market_pulse_import_run_id'
    ) THEN
        EXECUTE $sql$
            UPDATE public.market_pulse_import_failure
            SET market_pulse_pipeline_run_id = market_pulse_import_run_id
            WHERE market_pulse_pipeline_run_id IS NULL
        $sql$;

        IF EXISTS
        (
            SELECT 1
            FROM public.market_pulse_import_failure
            WHERE market_pulse_import_run_id IS NOT NULL
              AND market_pulse_pipeline_run_id IS DISTINCT FROM market_pulse_import_run_id
        ) THEN
            RAISE EXCEPTION 'Market Pulse failure foreign-key verification failed.';
        END IF;
    END IF;
END
$migration$;

ALTER TABLE public.market_pulse_import_failure
    DROP CONSTRAINT IF EXISTS fk_market_pulse_import_failure_run;
ALTER TABLE public.market_pulse_import_failure
    DROP CONSTRAINT IF EXISTS fk_market_pulse_import_failure_pipeline_run;
ALTER TABLE public.market_pulse_import_failure
    DROP COLUMN IF EXISTS market_pulse_import_run_id;
ALTER TABLE public.market_pulse_import_failure
    ADD CONSTRAINT fk_market_pulse_import_failure_pipeline_run
        FOREIGN KEY (market_pulse_pipeline_run_id)
        REFERENCES public.market_pulse_pipeline_run(market_pulse_pipeline_run_id)
        ON DELETE SET NULL;

DROP TABLE IF EXISTS public.market_pulse_refresh_operation;
DROP TABLE IF EXISTS public.market_pulse_publication_history_state;
DROP TABLE IF EXISTS public.market_pulse_import_run;

DROP INDEX IF EXISTS public.ix_market_pulse_import_run_started_status;
DROP INDEX IF EXISTS public.uq_market_pulse_refresh_operation_active;
DROP INDEX IF EXISTS public.ix_market_pulse_refresh_operation_requested_at;

CREATE INDEX IF NOT EXISTS ix_market_pulse_pipeline_type_started_status
    ON public.market_pulse_pipeline_run(operation_type, started_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_market_pulse_pipeline_requested_at
    ON public.market_pulse_pipeline_run(requested_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS uq_market_pulse_pipeline_active_refresh
    ON public.market_pulse_pipeline_run ((1))
    WHERE operation_type = 'refresh' AND status IN ('queued', 'crawling', 'importing');
CREATE UNIQUE INDEX IF NOT EXISTS uq_market_pulse_pipeline_history_state
    ON public.market_pulse_pipeline_run ((1))
    WHERE operation_type = 'history_sync';

-- These legacy tables have no application service/controller consumers.
DROP TABLE IF EXISTS public.payment_transaction;
DROP TABLE IF EXISTS public.invoice;
DROP TABLE IF EXISTS public.user_insight;

COMMIT;
