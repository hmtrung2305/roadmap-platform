-- ============================================================
-- 023-market-pulse-admin-ops.sql
-- Adds admin operations storage for Job Market Pulse.
-- ============================================================

CREATE TABLE IF NOT EXISTS public.market_pulse_crawl_run
(
    market_pulse_crawl_run_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    source_name varchar(80) NOT NULL DEFAULT 'all',
    status varchar(40) NOT NULL DEFAULT 'running',
    mode varchar(40) NOT NULL DEFAULT 'scheduled',
    started_at timestamptz NOT NULL DEFAULT now(),
    finished_at timestamptz,
    duration_ms int,
    fetched_count int NOT NULL DEFAULT 0,
    saved_count int NOT NULL DEFAULT 0,
    duplicate_count int NOT NULL DEFAULT 0,
    failed_count int NOT NULL DEFAULT 0,
    stopped_reason text,
    error_summary text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_market_pulse_crawl_run_counts
        CHECK (
            fetched_count >= 0 AND
            saved_count >= 0 AND
            duplicate_count >= 0 AND
            failed_count >= 0
        )
);

CREATE INDEX IF NOT EXISTS ix_market_pulse_crawl_run_started_status
    ON public.market_pulse_crawl_run(started_at DESC, status);

CREATE INDEX IF NOT EXISTS ix_market_pulse_crawl_run_source
    ON public.market_pulse_crawl_run(source_name);

CREATE TABLE IF NOT EXISTS public.market_pulse_failed_item
(
    market_pulse_failed_item_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    market_pulse_crawl_run_id uuid REFERENCES public.market_pulse_crawl_run(market_pulse_crawl_run_id) ON DELETE SET NULL,
    source_name varchar(80) NOT NULL DEFAULT 'unknown',
    url varchar(500),
    stage varchar(40) NOT NULL DEFAULT 'unknown',
    error_code varchar(80) NOT NULL DEFAULT 'UNKNOWN',
    error_message text NOT NULL,
    error_detail text,
    raw_payload jsonb,
    retry_count int NOT NULL DEFAULT 0,
    last_retry_at timestamptz,
    status varchar(30) NOT NULL DEFAULT 'open',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_market_pulse_failed_item_retry_count
        CHECK (retry_count >= 0),

    CONSTRAINT chk_market_pulse_failed_item_raw_payload_json
        CHECK (raw_payload IS NULL OR jsonb_typeof(raw_payload) IN ('object', 'array'))
);

CREATE INDEX IF NOT EXISTS ix_market_pulse_failed_item_status_created
    ON public.market_pulse_failed_item(status, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_market_pulse_failed_item_source_stage
    ON public.market_pulse_failed_item(source_name, stage);

CREATE TABLE IF NOT EXISTS public.market_pulse_classifier_keyword_mapping
(
    market_pulse_classifier_keyword_mapping_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    keyword varchar(160) NOT NULL,
    category varchar(100) NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    weight numeric(8,2) NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_market_pulse_classifier_keyword_category
        UNIQUE (keyword, category),

    CONSTRAINT chk_market_pulse_classifier_weight
        CHECK (weight > 0)
);

CREATE INDEX IF NOT EXISTS ix_market_pulse_classifier_enabled_category
    ON public.market_pulse_classifier_keyword_mapping(is_enabled, category);

CREATE TABLE IF NOT EXISTS public.market_pulse_source_health
(
    market_pulse_source_health_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    source_name varchar(80) NOT NULL,
    status varchar(40) NOT NULL DEFAULT 'unknown',
    last_success_at timestamptz,
    last_failure_at timestamptz,
    consecutive_failures int NOT NULL DEFAULT 0,
    last_run_id uuid,
    last_error_summary text,
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_market_pulse_source_health_source UNIQUE (source_name),
    CONSTRAINT chk_market_pulse_source_health_failures CHECK (consecutive_failures >= 0)
);

INSERT INTO public.market_pulse_classifier_keyword_mapping (keyword, category, weight)
VALUES
    ('backend', 'Backend', 1),
    ('java', 'Backend', 1),
    ('spring', 'Backend', 1),
    ('asp.net', 'Backend', 1),
    ('frontend', 'Frontend', 1),
    ('react', 'Frontend', 1),
    ('vue', 'Frontend', 1),
    ('angular', 'Frontend', 1),
    ('fullstack', 'Fullstack', 1),
    ('full stack', 'Fullstack', 1),
    ('mobile', 'Mobile', 1),
    ('android', 'Mobile', 1),
    ('ios', 'Mobile', 1),
    ('devops', 'DevOps', 1),
    ('kubernetes', 'DevOps', 1),
    ('data engineer', 'Data', 1),
    ('data analyst', 'Data', 1),
    ('machine learning', 'AI/ML', 1),
    ('ai engineer', 'AI/ML', 1),
    ('qa', 'QA/Testing', 1),
    ('tester', 'QA/Testing', 1),
    ('security', 'Security', 1),
    ('ui ux', 'UI/UX', 1),
    ('product manager', 'Project/Product Management', 1),
    ('project manager', 'Project/Product Management', 1)
ON CONFLICT (keyword, category) DO NOTHING;

INSERT INTO public.permission (permission_name)
VALUES
    ('market_pulse.manage.any'),
    ('market_pulse.view.catalog')
ON CONFLICT (permission_name) DO NOTHING;

INSERT INTO public.permission_role (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM public.role r
JOIN public.permission p ON p.permission_name IN (
    'market_pulse.manage.any',
    'market_pulse.view.catalog')
WHERE r.role_name = 'admin'
ON CONFLICT DO NOTHING;
