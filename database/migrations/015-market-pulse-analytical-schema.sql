-- ============================================================
-- 015-market-pulse-analytical-schema.sql
-- Adds the Phase 2 analytical storage layer for Market Pulse.
-- ============================================================

CREATE TABLE IF NOT EXISTS public.job_posting_version
(
    job_posting_version_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_posting_id uuid NOT NULL REFERENCES public.job_posting(job_posting_id) ON DELETE CASCADE,
    content_hash varchar(64) NOT NULL,
    title varchar(250) NOT NULL,
    company_name varchar(160),
    category varchar(100),
    location varchar(160),
    salary varchar(100),
    experience varchar(100),
    description text NOT NULL,
    requirements jsonb NOT NULL DEFAULT '[]'::jsonb,
    specialties jsonb NOT NULL DEFAULT '[]'::jsonb,
    benefits jsonb NOT NULL DEFAULT '[]'::jsonb,
    skills jsonb NOT NULL DEFAULT '[]'::jsonb,
    observed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_job_posting_version_hash
        UNIQUE (job_posting_id, content_hash),

    CONSTRAINT chk_job_posting_version_requirements_json_array
        CHECK (jsonb_typeof(requirements) = 'array'),

    CONSTRAINT chk_job_posting_version_specialties_json_array
        CHECK (jsonb_typeof(specialties) = 'array'),

    CONSTRAINT chk_job_posting_version_benefits_json_array
        CHECK (jsonb_typeof(benefits) = 'array'),

    CONSTRAINT chk_job_posting_version_skills_json_array
        CHECK (jsonb_typeof(skills) = 'array')
);

CREATE TABLE IF NOT EXISTS public.job_posting_observation
(
    job_posting_observation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_posting_id uuid NOT NULL REFERENCES public.job_posting(job_posting_id) ON DELETE CASCADE,
    snapshot_date date NOT NULL,
    source_name varchar(80) NOT NULL,
    observation_status varchar(32) NOT NULL,
    content_hash varchar(64) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    observed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_job_posting_observation
        UNIQUE (job_posting_id, snapshot_date, observation_status)
);

CREATE TABLE IF NOT EXISTS public.skill_taxonomy
(
    skill_taxonomy_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name varchar(100) NOT NULL,
    skill_slug varchar(120) NOT NULL,
    category varchar(100),
    aliases jsonb NOT NULL DEFAULT '[]'::jsonb,
    platform_skill_slug varchar(120),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_skill_taxonomy_slug UNIQUE (skill_slug),

    CONSTRAINT chk_skill_taxonomy_aliases_json_array
        CHECK (jsonb_typeof(aliases) = 'array')
);

CREATE TABLE IF NOT EXISTS public.job_skill_mention
(
    job_skill_mention_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_posting_id uuid NOT NULL REFERENCES public.job_posting(job_posting_id) ON DELETE CASCADE,
    skill_taxonomy_id uuid NOT NULL REFERENCES public.skill_taxonomy(skill_taxonomy_id) ON DELETE CASCADE,
    source_name varchar(80) NOT NULL,
    skill_name varchar(100) NOT NULL,
    skill_slug varchar(120) NOT NULL,
    mention_source varchar(40) NOT NULL DEFAULT 'normalized',
    snapshot_date date NOT NULL,
    observed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_job_skill_mention
        UNIQUE (job_posting_id, skill_slug, mention_source)
);

CREATE TABLE IF NOT EXISTS public.job_market_daily_snapshot
(
    job_market_daily_snapshot_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    snapshot_date date NOT NULL,
    source_name varchar(80) NOT NULL DEFAULT 'all',
    category varchar(100),
    location varchar(160),
    skill_slug varchar(120),
    skill_name varchar(100),
    active_job_count int NOT NULL DEFAULT 0,
    new_job_count int NOT NULL DEFAULT 0,
    observed_job_count int NOT NULL DEFAULT 0,
    mention_count int NOT NULL DEFAULT 0,
    salary_sample_count int NOT NULL DEFAULT 0,
    salary_min int,
    salary_max int,
    experience_min_years numeric(5,2),
    experience_max_years numeric(5,2),
    sample_size int NOT NULL DEFAULT 0,
    confidence varchar(20) NOT NULL DEFAULT 'low',
    generated_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_job_market_daily_snapshot_counts
        CHECK (
            active_job_count >= 0 AND
            new_job_count >= 0 AND
            observed_job_count >= 0 AND
            mention_count >= 0 AND
            salary_sample_count >= 0 AND
            sample_size >= 0
        )
);

CREATE TABLE IF NOT EXISTS public.market_pulse_insight_snapshot
(
    market_pulse_insight_snapshot_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    snapshot_date date NOT NULL,
    source_name varchar(80) NOT NULL DEFAULT 'all',
    insight_key varchar(120) NOT NULL,
    insight_type varchar(60) NOT NULL,
    period_days int NOT NULL DEFAULT 1,
    sample_size int NOT NULL DEFAULT 0,
    confidence varchar(20) NOT NULL DEFAULT 'low',
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    generated_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_market_pulse_insight_snapshot
        UNIQUE (snapshot_date, source_name, insight_key),

    CONSTRAINT chk_market_pulse_insight_payload_json_object
        CHECK (jsonb_typeof(payload) = 'object')
);

CREATE INDEX IF NOT EXISTS ix_job_posting_version_posting_observed
    ON public.job_posting_version(job_posting_id, observed_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_observation_source_date
    ON public.job_posting_observation(source_name, snapshot_date);

CREATE INDEX IF NOT EXISTS ix_skill_taxonomy_active_slug
    ON public.skill_taxonomy(is_active, skill_slug);

CREATE INDEX IF NOT EXISTS ix_job_skill_mention_skill_date
    ON public.job_skill_mention(skill_slug, snapshot_date);

CREATE INDEX IF NOT EXISTS ix_job_skill_mention_source_date
    ON public.job_skill_mention(source_name, snapshot_date);

CREATE INDEX IF NOT EXISTS ix_job_market_daily_snapshot_date_source
    ON public.job_market_daily_snapshot(snapshot_date, source_name);

CREATE INDEX IF NOT EXISTS ix_job_market_daily_snapshot_skill_date
    ON public.job_market_daily_snapshot(skill_slug, snapshot_date);

CREATE UNIQUE INDEX IF NOT EXISTS uq_job_market_daily_snapshot_grain
    ON public.job_market_daily_snapshot(
        snapshot_date,
        source_name,
        COALESCE(category, ''),
        COALESCE(location, ''),
        COALESCE(skill_slug, '')
    );

CREATE INDEX IF NOT EXISTS ix_market_pulse_insight_snapshot_type_date
    ON public.market_pulse_insight_snapshot(insight_type, snapshot_date);
