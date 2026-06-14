-- ============================================================
-- 005-market-pulse.sql
-- Purpose:
--   Add daily IT job market scraping storage, skill trend
--   snapshots, and job lifecycle tracking for Market Pulse.
-- Notes:
--   This file combines the previous 005-market-pulse.sql and
--   006-market-pulse-job-lifecycle.sql into one idempotent migration.
-- ============================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.job_portal_source
(
    job_portal_source_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(80) NOT NULL,
    base_url text NOT NULL,
    search_url_template text NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    last_scraped_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_job_portal_source_name
        UNIQUE (name)
);

CREATE TABLE IF NOT EXISTS public.job_posting
(
    job_posting_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_portal_source_id uuid NOT NULL,
    external_id varchar(120) NOT NULL,
    title varchar(250) NOT NULL,
    company_name varchar(160),
    location varchar(160),
    url text NOT NULL,
    description text NOT NULL,
    published_at timestamptz,
    expires_at timestamptz,
    content_hash varchar(64) NOT NULL DEFAULT '',
    lifecycle_status varchar(32) NOT NULL DEFAULT 'active',
    is_active boolean NOT NULL DEFAULT true,
    missing_scan_count int NOT NULL DEFAULT 0,
    seen_count int NOT NULL DEFAULT 1,
    updated_scan_count int NOT NULL DEFAULT 0,
    first_seen_at timestamptz NOT NULL DEFAULT now(),
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    last_checked_at timestamptz NOT NULL DEFAULT now(),
    last_changed_at timestamptz,
    closed_detected_at timestamptz,
    scraped_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_job_posting_source
        FOREIGN KEY (job_portal_source_id)
        REFERENCES public.job_portal_source(job_portal_source_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_job_posting_source_external
        UNIQUE (job_portal_source_id, external_id)
);

-- Compatibility block: if an older partial Market Pulse table already exists,
-- add lifecycle columns without requiring a separate 006 migration.
ALTER TABLE public.job_posting
    ADD COLUMN IF NOT EXISTS expires_at timestamptz,
    ADD COLUMN IF NOT EXISTS content_hash varchar(64) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS lifecycle_status varchar(32) NOT NULL DEFAULT 'active',
    ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS missing_scan_count int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS seen_count int NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS updated_scan_count int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS first_seen_at timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS last_seen_at timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS last_checked_at timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS last_changed_at timestamptz,
    ADD COLUMN IF NOT EXISTS closed_detected_at timestamptz;

UPDATE public.job_posting
SET
    first_seen_at = COALESCE(scraped_at, created_at, first_seen_at, now()),
    last_seen_at = COALESCE(scraped_at, updated_at, last_seen_at, now()),
    last_checked_at = COALESCE(scraped_at, updated_at, last_checked_at, now()),
    content_hash = CASE
        WHEN content_hash = '' THEN encode(digest(coalesce(title, '') || E'\n' || coalesce(company_name, '') || E'\n' || coalesce(location, '') || E'\n' || coalesce(description, ''), 'sha256'), 'hex')
        ELSE content_hash
    END;

CREATE TABLE IF NOT EXISTS public.job_posting_daily_snapshot
(
    job_posting_daily_snapshot_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_posting_id uuid NOT NULL,
    snapshot_date date NOT NULL,
    source_name varchar(80) NOT NULL,
    observation_status varchar(32) NOT NULL,
    content_hash varchar(64) NOT NULL,
    observed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_job_posting_daily_snapshot_posting
        FOREIGN KEY (job_posting_id)
        REFERENCES public.job_posting(job_posting_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_job_posting_daily_snapshot
        UNIQUE (job_posting_id, snapshot_date)
);

CREATE TABLE IF NOT EXISTS public.skill_trend_snapshot
(
    skill_trend_snapshot_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    snapshot_date date NOT NULL,
    skill_name varchar(100) NOT NULL,
    skill_slug varchar(120) NOT NULL,
    source_name varchar(80) NOT NULL DEFAULT 'all',
    mention_count int NOT NULL DEFAULT 0,
    posting_count int NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_skill_trend_snapshot
        UNIQUE (skill_slug, snapshot_date, source_name),

    CONSTRAINT chk_skill_trend_snapshot_counts
        CHECK (mention_count >= 0 AND posting_count >= 0)
);

CREATE INDEX IF NOT EXISTS ix_job_posting_scraped_at
    ON public.job_posting(scraped_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_title
    ON public.job_posting(title);

CREATE INDEX IF NOT EXISTS ix_job_posting_active_last_seen
    ON public.job_posting(is_active, last_seen_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_lifecycle_status
    ON public.job_posting(lifecycle_status);

CREATE INDEX IF NOT EXISTS ix_job_posting_daily_snapshot_date
    ON public.job_posting_daily_snapshot(snapshot_date);

CREATE INDEX IF NOT EXISTS ix_skill_trend_snapshot_date
    ON public.skill_trend_snapshot(snapshot_date);

COMMIT;
