-- ============================================================
-- Consolidated database schema
-- Represents the final schema after migrations 002 through 016.
-- Intended for provisioning a new PostgreSQL database.
-- ============================================================

-- =========================
-- User, Role, Permission
-- =========================
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS public.permission
(
    permission_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_name varchar(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public.role
(
    role_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name varchar(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public."user"
(
    user_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username varchar(50) UNIQUE NOT NULL,
    username_normalized varchar(50) UNIQUE NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,

    CONSTRAINT chk_user_status
        CHECK (status in ('active', 'pending_verification', 'suspended', 'deleted'))
);

CREATE TABLE IF NOT EXISTS public.user_profile
(
    user_id uuid PRIMARY KEY,

    display_name varchar(50),
    phone_number varchar(32),
    headline varchar(150),
    bio varchar(500),
    location varchar(100),

    avatar_url text,
    cover_image_url text,

    career_goal varchar(150),
    "current_role" varchar(100),

    public_email varchar(254),
    github_url text,
    linkedin_url text,
    resume_url text,
    personal_website_url text,

    is_public bool NOT NULL DEFAULT false,

    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_profile_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.user_activity_stats
(
    user_id uuid PRIMARY KEY,

    current_streak integer NOT NULL DEFAULT 0,
    longest_streak integer NOT NULL DEFAULT 0,
    last_interaction timestamptz,

    CONSTRAINT fk_user_activity_stats_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE
);

-- =========================
-- Market Pulse
-- =========================
CREATE TABLE IF NOT EXISTS public.job_portal_source
(
    job_portal_source_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(80) NOT NULL UNIQUE,
    base_url text NOT NULL,
    search_url_template text NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    last_scraped_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.job_posting
(
    job_posting_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_portal_source_id uuid NOT NULL,
    external_id varchar(120) NOT NULL,
    source_job_id varchar(120),
    title varchar(250) NOT NULL,
    company_name varchar(160),
    category varchar(100),
    location varchar(160),
    salary varchar(100),
    experience varchar(100),
    url text NOT NULL,
    description text NOT NULL,
    published_at timestamptz,
    post_date_text varchar(80),
    source_updated_at timestamptz,
    expires_at timestamptz,
    requirements jsonb NOT NULL DEFAULT '[]'::jsonb,
    specialties jsonb NOT NULL DEFAULT '[]'::jsonb,
    benefits jsonb NOT NULL DEFAULT '[]'::jsonb,
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
        UNIQUE (job_portal_source_id, external_id),

    CONSTRAINT chk_job_posting_requirements_json_array
        CHECK (jsonb_typeof(requirements) = 'array'),

    CONSTRAINT chk_job_posting_specialties_json_array
        CHECK (jsonb_typeof(specialties) = 'array'),

    CONSTRAINT chk_job_posting_benefits_json_array
        CHECK (jsonb_typeof(benefits) = 'array')
);

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

CREATE INDEX IF NOT EXISTS ix_job_posting_scraped_at
    ON public.job_posting(scraped_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_title
    ON public.job_posting(title);

CREATE INDEX IF NOT EXISTS ix_job_posting_active_last_seen
    ON public.job_posting(is_active, last_seen_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_lifecycle_status
    ON public.job_posting(lifecycle_status);

CREATE INDEX IF NOT EXISTS ix_job_posting_source_job_id
    ON public.job_posting(source_job_id);

CREATE INDEX IF NOT EXISTS ix_job_posting_category
    ON public.job_posting(category);

CREATE INDEX IF NOT EXISTS ix_job_posting_published_at
    ON public.job_posting(published_at);

CREATE INDEX IF NOT EXISTS ix_job_posting_daily_snapshot_date
    ON public.job_posting_daily_snapshot(snapshot_date);
    
CREATE INDEX IF NOT EXISTS ix_skill_trend_snapshot_date
    ON public.skill_trend_snapshot(snapshot_date);

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

CREATE TABLE IF NOT EXISTS public.ai_credit_plan
(
    plan_code varchar(30) PRIMARY KEY,
    daily_credit_limit int NOT NULL,
    monthly_credit_limit int,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_ai_credit_plan_daily_limit
        CHECK (daily_credit_limit >= 0),

    CONSTRAINT chk_ai_credit_plan_monthly_limit
        CHECK (monthly_credit_limit IS NULL OR monthly_credit_limit >= 0)
);

INSERT INTO public.ai_credit_plan
    (plan_code, daily_credit_limit, monthly_credit_limit, description)
VALUES
    ('free', 5, NULL, 'Default experimental plan for regular users.'),
    ('premium', 100, NULL, 'Higher daily credit limit for future paid users.'),
    ('admin', 1000, NULL, 'High daily credit limit for administrators and internal testing.')
ON CONFLICT (plan_code) DO UPDATE
SET
    daily_credit_limit = EXCLUDED.daily_credit_limit,
    monthly_credit_limit = EXCLUDED.monthly_credit_limit,
    description = EXCLUDED.description;

CREATE TABLE IF NOT EXISTS public.user_ai_credit_plan
(
    user_id uuid PRIMARY KEY,
    plan_code varchar(30) NOT NULL,
    expires_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_ai_credit_plan_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_ai_credit_plan_plan_code
        FOREIGN KEY (plan_code)
        REFERENCES public.ai_credit_plan(plan_code)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS public.ai_credit_usage
(
    usage_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    feature_name varchar(80) NOT NULL,
    credit_cost int NOT NULL DEFAULT 1,
    request_ref_id uuid,
    metadata jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_ai_credit_usage_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_ai_credit_usage_credit_cost
        CHECK (credit_cost > 0)
);

CREATE INDEX IF NOT EXISTS idx_ai_credit_usage_user_created_at
    ON public.ai_credit_usage(user_id, created_at);

CREATE INDEX IF NOT EXISTS idx_ai_credit_usage_feature_created_at
    ON public.ai_credit_usage(feature_name, created_at);

CREATE TABLE IF NOT EXISTS public.permission_role
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_id uuid NOT NULL,
    role_id uuid NOT NULL,

    CONSTRAINT fk_permission_role_permission_id
        FOREIGN KEY (permission_id)
        REFERENCES public.permission(permission_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_permission_role_role_id
        FOREIGN KEY (role_id)
        REFERENCES public.role(role_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_permission_role
        UNIQUE (permission_id, role_id)
);

CREATE TABLE IF NOT EXISTS public.user_role
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id uuid NOT NULL,
    user_id uuid NOT NULL,

    CONSTRAINT fk_user_role_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_role_role_id
        FOREIGN KEY (role_id)
        REFERENCES public.role(role_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_role
        UNIQUE (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS public.user_auth_provider
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    email varchar(254),
    password_hash text,
    provider varchar(100) NOT NULL,
    provider_user_id varchar(255) NOT NULL,
    provider_username varchar(50),
    created_at timestamptz NOT NULL DEFAULT now(),
    pending_email varchar(254),
    email_verified_at timestamptz,
    access_token text,

    CONSTRAINT fk_user_auth_provider_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_provider_identity
        UNIQUE (provider, provider_user_id),

    CONSTRAINT uq_user_provider
        UNIQUE (user_id, provider)
);

CREATE TABLE IF NOT EXISTS public.pending_local_registration
(
    pending_local_registration_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    username varchar(50) NOT NULL,
    username_normalized varchar(50) NOT NULL,

    email varchar(254) NOT NULL,
    password_hash text NOT NULL,

    expires_at timestamptz NOT NULL,
    used_at timestamptz,

    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_pending_local_registration_email_active
    ON public.pending_local_registration(email)
    WHERE used_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_pending_local_registration_expires_at
    ON public.pending_local_registration(expires_at)
    WHERE used_at IS NULL;

CREATE TABLE IF NOT EXISTS public.email_verification_token
(
    verification_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    user_id uuid,
    pending_local_registration_id uuid,

    provider varchar(50) NOT NULL,
    email varchar(255) NOT NULL,

    purpose varchar(50) NOT NULL,
    otp_hash text NOT NULL,

    expires_at timestamptz NOT NULL,
    used_at timestamptz,

    attempt_count int NOT NULL DEFAULT 0,
    max_attempts int NOT NULL DEFAULT 5,

    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_email_verification_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_email_verification_pending_local_registration_id
        FOREIGN KEY (pending_local_registration_id)
        REFERENCES public.pending_local_registration(pending_local_registration_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_email_verification_token_single_owner
        CHECK (
            (user_id IS NOT NULL AND pending_local_registration_id IS NULL)
            OR
            (user_id IS NULL AND pending_local_registration_id IS NOT NULL)
        )
);

CREATE INDEX IF NOT EXISTS ix_email_verification_token_pending_local_registration_id
    ON public.email_verification_token(pending_local_registration_id);

-- =========================
-- Roadmap
-- =========================

CREATE TABLE IF NOT EXISTS public.skill
(
    skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL,
    slug varchar(120) NOT NULL,
    description text,
    category varchar(100),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_name
    ON public.skill(name);

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_slug
    ON public.skill(slug);

CREATE TABLE IF NOT EXISTS public.career_role
(
    career_role_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL UNIQUE,
    slug varchar(120) NOT NULL UNIQUE,
    description text,
    category varchar(100),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.learning_resource
(
    learning_resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title varchar(200) NOT NULL,
    url text NOT NULL,
    resource_type varchar(30) NOT NULL,
    description text,
    provider varchar(100),
    difficulty_level varchar(30),
    language_code varchar(10) NOT NULL DEFAULT 'en',
    verification_status varchar(30) NOT NULL DEFAULT 'verified',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_learning_resource_type
        CHECK (resource_type IN (
            'documentation',
            'video',
            'course',
            'article',
            'book',
            'practice',
            'project',
            'other'
        )),

    CONSTRAINT chk_learning_resource_difficulty_level
        CHECK (
            difficulty_level IS NULL OR
            difficulty_level IN ('beginner', 'intermediate', 'advanced')
        ),

    CONSTRAINT chk_learning_resource_verification_status
        CHECK (verification_status IN ('pending', 'verified', 'broken', 'rejected'))
);

CREATE TABLE IF NOT EXISTS public.learning_resource_skill
(
    learning_resource_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    learning_resource_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    CONSTRAINT fk_learning_resource_skill_resource
        FOREIGN KEY (learning_resource_id)
        REFERENCES public.learning_resource(learning_resource_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_learning_resource_skill_skill
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_learning_resource_skill
        UNIQUE (learning_resource_id, skill_id)
);

CREATE TABLE IF NOT EXISTS public.roadmap
(
    roadmap_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    career_role_id uuid NOT NULL,
    owner_user_id uuid,
    title varchar(200) NOT NULL,
    description text,
    roadmap_type varchar(30) NOT NULL DEFAULT 'template',
    source_type varchar(30) NOT NULL DEFAULT 'static',
    visibility varchar(30) NOT NULL DEFAULT 'public',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_career_role
        FOREIGN KEY (career_role_id)
        REFERENCES public.career_role(career_role_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_roadmap_owner_user
        FOREIGN KEY (owner_user_id)
        REFERENCES public."user"(user_id)
        ON DELETE SET NULL,

    CONSTRAINT chk_roadmap_type
        CHECK (roadmap_type IN ('template', 'personal', 'fork')),

    CONSTRAINT chk_roadmap_source_type
        CHECK (source_type IN ('static', 'ai')),

    CONSTRAINT chk_roadmap_visibility
        CHECK (visibility IN ('public', 'private'))
);

CREATE TABLE IF NOT EXISTS public.roadmap_version
(
    roadmap_version_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_id uuid NOT NULL,
    version_number int NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'draft',
    title varchar(200) NOT NULL,
    description text,
    estimated_total_hours int,
    layout_direction varchar(20) NOT NULL DEFAULT 'TB',
    layout_algorithm varchar(50),
    generated_by_user_id uuid,
    generation_prompt text,
    generation_model varchar(100),
    generation_status varchar(30) NOT NULL DEFAULT 'none',
    generation_context jsonb NOT NULL DEFAULT '{}'::jsonb,
    generation_error text,
    published_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_version_roadmap
        FOREIGN KEY (roadmap_id)
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_version_generated_by_user
        FOREIGN KEY (generated_by_user_id)
        REFERENCES public."user"(user_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_roadmap_version_number
        UNIQUE (roadmap_id, version_number),

    CONSTRAINT chk_roadmap_version_status
        CHECK (status IN ('draft', 'published', 'archived')),

    CONSTRAINT chk_roadmap_version_layout_direction
        CHECK (layout_direction IN ('TB', 'BT', 'LR', 'RL')),

    CONSTRAINT chk_roadmap_version_layout_algorithm
        CHECK (layout_algorithm IS NULL OR layout_algorithm IN ('manual', 'dagre', 'elk', 'custom')),

    CONSTRAINT chk_roadmap_version_generation_status
        CHECK (generation_status IN ('none', 'generating', 'published', 'failed')),

    CONSTRAINT chk_roadmap_version_estimated_total_hours
        CHECK (estimated_total_hours IS NULL OR estimated_total_hours > 0)
);

CREATE TABLE IF NOT EXISTS public.roadmap_node
(
    roadmap_node_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_version_id uuid NOT NULL,
    parent_node_id uuid,
    slug varchar(120) NOT NULL,
    node_type varchar(30) NOT NULL,
    checkpoint_type varchar(30),
    selection_type varchar(30),
    required_count int,
    title varchar(200) NOT NULL,
    description text,
    reason text,
    order_index int NOT NULL DEFAULT 0,
    layout_role varchar(30) NOT NULL DEFAULT 'side',
    layout_group varchar(80),
    layout_rank int,
    layout_order int NOT NULL DEFAULT 0,
    estimated_hours int,
    difficulty_level varchar(30),
    priority int NOT NULL DEFAULT 0,
    position_x numeric(10,2),
    position_y numeric(10,2),
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_required boolean NOT NULL DEFAULT true,
    is_trackable boolean NOT NULL DEFAULT true,
    learning_outcomes jsonb NOT NULL DEFAULT '[]'::jsonb,
    completion_criteria jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    is_assessment_skill boolean NOT NULL DEFAULT false,

    CONSTRAINT fk_roadmap_node_version
        FOREIGN KEY (roadmap_version_id)
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_node_parent
        FOREIGN KEY (parent_node_id)
        REFERENCES public.roadmap_node(roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_roadmap_node_type
        CHECK (node_type IN (
            'phase',
            'topic',
            'choice_group',
            'choice_option',
            'checkpoint',
            'project',
            'resource_group'
        )),

    CONSTRAINT chk_roadmap_node_checkpoint_type
        CHECK (
            checkpoint_type IS NULL OR
            checkpoint_type IN ('completion', 'gate', 'assessment', 'project', 'review')
        ),

    CONSTRAINT chk_roadmap_node_selection_type
        CHECK (
            selection_type IS NULL OR
            selection_type IN ('complete_all', 'choose_one', 'choose_many')
        ),

    CONSTRAINT chk_roadmap_node_required_count
        CHECK (required_count IS NULL OR required_count > 0),

    CONSTRAINT chk_roadmap_node_layout_role
        CHECK (layout_role IN ('trunk', 'side', 'choice', 'checkpoint', 'hidden')),

    CONSTRAINT chk_roadmap_node_checkpoint_fields
        CHECK (node_type = 'checkpoint' OR checkpoint_type IS NULL),

    CONSTRAINT chk_roadmap_node_selection_fields
        CHECK (
            node_type = 'choice_group' OR
            (selection_type IS NULL AND required_count IS NULL)
        ),

    CONSTRAINT chk_roadmap_node_choice_group_rule
        CHECK (
            node_type <> 'choice_group' OR
            selection_type IN ('complete_all', 'choose_one', 'choose_many')
        ),

    CONSTRAINT chk_roadmap_node_choose_one_count
        CHECK (selection_type <> 'choose_one' OR required_count = 1),

    CONSTRAINT chk_roadmap_node_choose_many_count
        CHECK (selection_type <> 'choose_many' OR required_count IS NOT NULL),

    CONSTRAINT chk_roadmap_node_estimated_hours
        CHECK (estimated_hours IS NULL OR estimated_hours > 0),

    CONSTRAINT chk_roadmap_node_difficulty_level
        CHECK (
            difficulty_level IS NULL OR
            difficulty_level IN ('beginner', 'intermediate', 'advanced')
        ),

    CONSTRAINT uq_roadmap_version_node
        UNIQUE (roadmap_version_id, roadmap_node_id),

    CONSTRAINT uq_roadmap_node_version_slug
        UNIQUE (roadmap_version_id, slug)
);

-- Optional mapping: one node can teach multiple skills.
CREATE TABLE IF NOT EXISTS public.roadmap_node_skill
(
    roadmap_node_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_node_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    CONSTRAINT fk_roadmap_node_skill_node
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public.roadmap_node(roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_node_skill_skill
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_roadmap_node_skill
        UNIQUE (roadmap_node_id, skill_id)
);

CREATE TABLE IF NOT EXISTS public.roadmap_edge
(
    roadmap_edge_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_version_id uuid NOT NULL,
    from_node_id uuid NOT NULL,
    to_node_id uuid NOT NULL,
    edge_type varchar(30) NOT NULL DEFAULT 'dependency',
    dependency_type varchar(30) NOT NULL DEFAULT 'required',
    condition jsonb NOT NULL DEFAULT '{}'::jsonb,

    CONSTRAINT fk_roadmap_edge_version
        FOREIGN KEY (roadmap_version_id)
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_from_node
        FOREIGN KEY (roadmap_version_id, from_node_id)
        REFERENCES public.roadmap_node(roadmap_version_id, roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_to_node
        FOREIGN KEY (roadmap_version_id, to_node_id)
        REFERENCES public.roadmap_node(roadmap_version_id, roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_roadmap_edge
        UNIQUE (roadmap_version_id, from_node_id, to_node_id, edge_type),

    CONSTRAINT chk_roadmap_edge_not_self
        CHECK (from_node_id <> to_node_id),

    CONSTRAINT chk_roadmap_edge_type
        CHECK (edge_type IN ('sequence', 'contains', 'choice', 'dependency', 'unlock', 'recommendation')),

    CONSTRAINT chk_roadmap_edge_dependency_type
        CHECK (dependency_type IN ('required', 'recommended', 'optional'))
);

CREATE TABLE IF NOT EXISTS public.roadmap_node_resource
(
    roadmap_node_resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_node_id uuid NOT NULL,
    learning_resource_id uuid NOT NULL,
    order_index int NOT NULL DEFAULT 0,
    is_primary boolean NOT NULL DEFAULT false,

    CONSTRAINT fk_roadmap_node_resource_node
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public.roadmap_node(roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_node_resource_resource
        FOREIGN KEY (learning_resource_id)
        REFERENCES public.learning_resource(learning_resource_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_roadmap_node_resource
        UNIQUE (roadmap_node_id, learning_resource_id)
);

CREATE TABLE IF NOT EXISTS public.roadmap_enrollment
(
    roadmap_enrollment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    roadmap_version_id uuid NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'active',
    progress_percent numeric(5,2) NOT NULL DEFAULT 0,
    started_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_enrollment_user
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_enrollment_version
        FOREIGN KEY (roadmap_version_id)
        REFERENCES public.roadmap_version(roadmap_version_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_user_roadmap_version_enrollment
        UNIQUE (user_id, roadmap_version_id),

    CONSTRAINT chk_roadmap_enrollment_status
        CHECK (status IN ('active', 'paused', 'completed', 'abandoned')),

    CONSTRAINT chk_roadmap_enrollment_progress
        CHECK (progress_percent >= 0 AND progress_percent <= 100)
);

CREATE TABLE IF NOT EXISTS public.user_node_progress
(
    user_node_progress_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_enrollment_id uuid NOT NULL,
    roadmap_node_id uuid NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'pending',
    evidence_url text,
    learner_note text,
    started_at timestamptz,
    completed_at timestamptz,
    skipped_at timestamptz,
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_node_progress_enrollment
        FOREIGN KEY (roadmap_enrollment_id)
        REFERENCES public.roadmap_enrollment(roadmap_enrollment_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_node_progress_node
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public.roadmap_node(roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_enrollment_node_progress
        UNIQUE (roadmap_enrollment_id, roadmap_node_id),

    CONSTRAINT chk_user_node_progress_status
        CHECK (status IN ('pending', 'in_progress', 'completed', 'skipped'))
);

CREATE TABLE IF NOT EXISTS public.progress_event
(
    progress_event_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_enrollment_id uuid NOT NULL,
    roadmap_node_id uuid NOT NULL,
    user_id uuid NOT NULL,
    old_status varchar(30),
    new_status varchar(30) NOT NULL,
    idempotency_key varchar(100),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_progress_event_enrollment
        FOREIGN KEY (roadmap_enrollment_id)
        REFERENCES public.roadmap_enrollment(roadmap_enrollment_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_progress_event_node
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public.roadmap_node(roadmap_node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_progress_event_user
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_progress_event_old_status
        CHECK (
            old_status IS NULL OR
            old_status IN ('pending', 'in_progress', 'completed', 'skipped')
        ),

    CONSTRAINT chk_progress_event_new_status
        CHECK (new_status IN ('pending', 'in_progress', 'completed', 'skipped'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_progress_event_idempotency_not_null
    ON public.progress_event(roadmap_enrollment_id, idempotency_key)
    WHERE idempotency_key IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_skill_slug
    ON public.skill(slug);

CREATE INDEX IF NOT EXISTS ix_skill_category
    ON public.skill(category);

CREATE INDEX IF NOT EXISTS ix_career_role_slug
    ON public.career_role(slug);

CREATE INDEX IF NOT EXISTS ix_career_role_category
    ON public.career_role(category);

CREATE INDEX IF NOT EXISTS ix_learning_resource_type
    ON public.learning_resource(resource_type);

CREATE INDEX IF NOT EXISTS ix_learning_resource_provider
    ON public.learning_resource(provider);

CREATE INDEX IF NOT EXISTS ix_learning_resource_skill_resource
    ON public.learning_resource_skill(learning_resource_id);

CREATE INDEX IF NOT EXISTS ix_learning_resource_skill_skill
    ON public.learning_resource_skill(skill_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_career_role_id
    ON public.roadmap(career_role_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_owner_user_id
    ON public.roadmap(owner_user_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_type_visibility
    ON public.roadmap(roadmap_type, visibility);

CREATE INDEX IF NOT EXISTS ix_roadmap_source_type
    ON public.roadmap(source_type);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_roadmap_id
    ON public.roadmap_version(roadmap_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_status
    ON public.roadmap_version(status);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_generation_status
    ON public.roadmap_version(generation_status);

CREATE INDEX IF NOT EXISTS ix_roadmap_version_generated_by_user_id
    ON public.roadmap_version(generated_by_user_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_version_id
    ON public.roadmap_node(roadmap_version_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_slug
    ON public.roadmap_node(roadmap_version_id, slug);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_parent_id
    ON public.roadmap_node(parent_node_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_type
    ON public.roadmap_node(node_type);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_layout_role
    ON public.roadmap_node(layout_role);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_layout_group
    ON public.roadmap_node(roadmap_version_id, layout_group);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_layout_rank_order
    ON public.roadmap_node(roadmap_version_id, layout_rank, layout_order);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_checkpoint_type
    ON public.roadmap_node(checkpoint_type);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_position
    ON public.roadmap_node(roadmap_version_id, position_x, position_y);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_skill_node
    ON public.roadmap_node_skill(roadmap_node_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_skill_skill
    ON public.roadmap_node_skill(skill_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_edge_version_id
    ON public.roadmap_edge(roadmap_version_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_edge_type
    ON public.roadmap_edge(edge_type);

CREATE INDEX IF NOT EXISTS ix_roadmap_edge_from_node
    ON public.roadmap_edge(from_node_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_edge_to_node
    ON public.roadmap_edge(to_node_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_resource_node_id
    ON public.roadmap_node_resource(roadmap_node_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_node_resource_resource_id
    ON public.roadmap_node_resource(learning_resource_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_enrollment_user_id
    ON public.roadmap_enrollment(user_id);

CREATE INDEX IF NOT EXISTS ix_roadmap_enrollment_version_id
    ON public.roadmap_enrollment(roadmap_version_id);

CREATE INDEX IF NOT EXISTS ix_user_node_progress_enrollment_id
    ON public.user_node_progress(roadmap_enrollment_id);

CREATE INDEX IF NOT EXISTS ix_user_node_progress_node_id
    ON public.user_node_progress(roadmap_node_id);

CREATE INDEX IF NOT EXISTS ix_progress_event_enrollment_id
    ON public.progress_event(roadmap_enrollment_id);

CREATE INDEX IF NOT EXISTS ix_progress_event_user_id
    ON public.progress_event(user_id);


-- =========================
-- Skill Learning Modules
-- =========================

CREATE TABLE IF NOT EXISTS public.skill_module
(
    skill_module_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    slug varchar(200) NOT NULL,
    description text,
    difficulty_level varchar(30),
    estimated_hours numeric(5,2),
    status varchar(30) NOT NULL DEFAULT 'draft',
    created_by_user_id uuid,
    published_at timestamptz,
    archived_at timestamptz,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_skill
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_created_by_user
        FOREIGN KEY (created_by_user_id)
        REFERENCES public."user"(user_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_skill_module_slug
        UNIQUE (slug),

    CONSTRAINT chk_skill_module_status
        CHECK (status IN ('draft', 'published', 'archived')),

    CONSTRAINT chk_skill_module_difficulty_level
        CHECK (
            difficulty_level IS NULL OR
            difficulty_level IN ('beginner', 'intermediate', 'advanced')
        ),

    CONSTRAINT chk_skill_module_estimated_hours
        CHECK (estimated_hours IS NULL OR estimated_hours >= 0)
);

CREATE TABLE IF NOT EXISTS public.skill_module_lesson
(
    skill_module_lesson_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    slug varchar(200) NOT NULL,
    summary text,
    order_index int NOT NULL,
    estimated_hours numeric(5,2),
    markdown_file_key text NOT NULL,
    markdown_file_name varchar(255),
    content_hash text,
    content_size_bytes bigint,
    content_version int NOT NULL DEFAULT 1,
    indexing_status varchar(30) NOT NULL DEFAULT 'pending',
    indexed_at timestamptz,
    indexing_error text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_lesson_module
        FOREIGN KEY (skill_module_id)
        REFERENCES public.skill_module(skill_module_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_lesson_slug
        UNIQUE (skill_module_id, slug),

    CONSTRAINT uq_skill_module_lesson_order
        UNIQUE (skill_module_id, order_index),

    CONSTRAINT chk_skill_module_lesson_order
        CHECK (order_index > 0),

    CONSTRAINT chk_skill_module_lesson_estimated_hours
        CHECK (estimated_hours IS NULL OR estimated_hours >= 0),

    CONSTRAINT chk_skill_module_lesson_content_version
        CHECK (content_version > 0),

    CONSTRAINT chk_skill_module_lesson_content_size
        CHECK (content_size_bytes IS NULL OR content_size_bytes >= 0),

    CONSTRAINT chk_skill_module_lesson_indexing_status
        CHECK (indexing_status IN ('pending', 'indexing', 'indexed', 'failed', 'needs_reindex'))
);

CREATE TABLE IF NOT EXISTS public.skill_module_quiz
(
    skill_module_quiz_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    description text,
    passing_score_percent numeric(5,2) NOT NULL DEFAULT 70,
    max_attempts int,
    status varchar(30) NOT NULL DEFAULT 'draft',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_quiz_module
        FOREIGN KEY (skill_module_id)
        REFERENCES public.skill_module(skill_module_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_quiz_module
        UNIQUE (skill_module_id),

    CONSTRAINT chk_skill_module_quiz_status
        CHECK (status IN ('draft', 'published', 'archived')),

    CONSTRAINT chk_skill_module_quiz_score
        CHECK (passing_score_percent >= 0 AND passing_score_percent <= 100),

    CONSTRAINT chk_skill_module_quiz_max_attempts
        CHECK (max_attempts IS NULL OR max_attempts > 0)
);

CREATE TABLE IF NOT EXISTS public.skill_module_quiz_question
(
    skill_module_quiz_question_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_quiz_id uuid NOT NULL,
    question_text text NOT NULL,
    question_type varchar(30) NOT NULL DEFAULT 'single_choice',
    explanation text,
    order_index int NOT NULL,
    points int NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_quiz_question_quiz
        FOREIGN KEY (skill_module_quiz_id)
        REFERENCES public.skill_module_quiz(skill_module_quiz_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_quiz_question_order
        UNIQUE (skill_module_quiz_id, order_index),

    CONSTRAINT chk_skill_module_question_type
        CHECK (question_type IN ('single_choice')),

    CONSTRAINT chk_skill_module_question_order
        CHECK (order_index > 0),

    CONSTRAINT chk_skill_module_question_points
        CHECK (points > 0)
);

CREATE TABLE IF NOT EXISTS public.skill_module_quiz_option
(
    skill_module_quiz_option_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_quiz_question_id uuid NOT NULL,
    option_text text NOT NULL,
    is_correct boolean NOT NULL DEFAULT false,
    explanation text,
    order_index int NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_quiz_option_question
        FOREIGN KEY (skill_module_quiz_question_id)
        REFERENCES public.skill_module_quiz_question(skill_module_quiz_question_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_quiz_option_order
        UNIQUE (skill_module_quiz_question_id, order_index),

    CONSTRAINT chk_skill_module_option_order
        CHECK (order_index > 0)
);

CREATE TABLE IF NOT EXISTS public.skill_module_enrollment
(
    skill_module_enrollment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    skill_module_id uuid NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'in_progress',
    started_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    last_accessed_lesson_id uuid,
    progress_percent numeric(5,2) NOT NULL DEFAULT 0,
    lesson_progress jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_enrollment_user
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_enrollment_module
        FOREIGN KEY (skill_module_id)
        REFERENCES public.skill_module(skill_module_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_enrollment_last_lesson
        FOREIGN KEY (last_accessed_lesson_id)
        REFERENCES public.skill_module_lesson(skill_module_lesson_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_skill_module_enrollment_user_module
        UNIQUE (user_id, skill_module_id),

    CONSTRAINT chk_skill_module_enrollment_status
        CHECK (status IN ('in_progress', 'completed')),

    CONSTRAINT chk_skill_module_enrollment_progress
        CHECK (progress_percent >= 0 AND progress_percent <= 100),

    CONSTRAINT chk_skill_module_enrollment_lesson_progress_object
        CHECK (jsonb_typeof(lesson_progress) = 'object')
);

CREATE TABLE IF NOT EXISTS public.skill_module_quiz_attempt
(
    skill_module_quiz_attempt_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_quiz_id uuid NOT NULL,
    skill_module_enrollment_id uuid NOT NULL,
    user_id uuid NOT NULL,
    attempt_no int NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'in_progress',
    started_at timestamptz NOT NULL DEFAULT now(),
    submitted_at timestamptz,
    score_percent numeric(5,2),
    earned_points int,
    total_points int,
    passed boolean,

    CONSTRAINT fk_skill_module_quiz_attempt_quiz
        FOREIGN KEY (skill_module_quiz_id)
        REFERENCES public.skill_module_quiz(skill_module_quiz_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_quiz_attempt_enrollment
        FOREIGN KEY (skill_module_enrollment_id)
        REFERENCES public.skill_module_enrollment(skill_module_enrollment_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_quiz_attempt_user
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_quiz_attempt_no
        UNIQUE (skill_module_quiz_id, user_id, attempt_no),

    CONSTRAINT chk_skill_module_attempt_no
        CHECK (attempt_no > 0),

    CONSTRAINT chk_skill_module_attempt_status
        CHECK (status IN ('in_progress', 'submitted', 'abandoned')),

    CONSTRAINT chk_skill_module_attempt_score
        CHECK (score_percent IS NULL OR (score_percent >= 0 AND score_percent <= 100)),

    CONSTRAINT chk_skill_module_attempt_points
        CHECK (
            (earned_points IS NULL OR earned_points >= 0) AND
            (total_points IS NULL OR total_points >= 0)
        )
);

CREATE TABLE IF NOT EXISTS public.skill_module_quiz_answer
(
    skill_module_quiz_answer_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_quiz_attempt_id uuid NOT NULL,
    skill_module_quiz_question_id uuid NOT NULL,
    selected_option_id uuid,
    is_correct boolean NOT NULL DEFAULT false,
    earned_points int NOT NULL DEFAULT 0,
    answered_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_quiz_answer_attempt
        FOREIGN KEY (skill_module_quiz_attempt_id)
        REFERENCES public.skill_module_quiz_attempt(skill_module_quiz_attempt_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_quiz_answer_question
        FOREIGN KEY (skill_module_quiz_question_id)
        REFERENCES public.skill_module_quiz_question(skill_module_quiz_question_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_quiz_answer_option
        FOREIGN KEY (selected_option_id)
        REFERENCES public.skill_module_quiz_option(skill_module_quiz_option_id)
        ON DELETE SET NULL,

    CONSTRAINT uq_skill_module_quiz_answer_question
        UNIQUE (skill_module_quiz_attempt_id, skill_module_quiz_question_id),

    CONSTRAINT chk_skill_module_quiz_answer_points
        CHECK (earned_points >= 0)
);

CREATE TABLE IF NOT EXISTS public.skill_module_chunk
(
    skill_module_chunk_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_module_id uuid NOT NULL,
    skill_module_lesson_id uuid NOT NULL,
    chunk_index int NOT NULL,
    heading text,
    content text NOT NULL,
    embedding vector(3072),
    token_count int,
    content_hash text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_skill_module_chunk_module
        FOREIGN KEY (skill_module_id)
        REFERENCES public.skill_module(skill_module_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_module_chunk_lesson
        FOREIGN KEY (skill_module_lesson_id)
        REFERENCES public.skill_module_lesson(skill_module_lesson_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_module_chunk_lesson_index
        UNIQUE (skill_module_lesson_id, chunk_index),

    CONSTRAINT chk_skill_module_chunk_index
        CHECK (chunk_index > 0),

    CONSTRAINT chk_skill_module_chunk_token_count
        CHECK (token_count IS NULL OR token_count >= 0)
);

CREATE INDEX IF NOT EXISTS ix_skill_module_skill_id
    ON public.skill_module(skill_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_created_by_user_id
    ON public.skill_module(created_by_user_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_status_updated_at
    ON public.skill_module(status, updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_skill_module_published_at
    ON public.skill_module(published_at DESC);

CREATE INDEX IF NOT EXISTS ix_skill_module_lesson_module_order
    ON public.skill_module_lesson(skill_module_id, order_index);

CREATE INDEX IF NOT EXISTS ix_skill_module_lesson_indexing_status
    ON public.skill_module_lesson(skill_module_id, indexing_status);

CREATE INDEX IF NOT EXISTS ix_skill_module_quiz_status
    ON public.skill_module_quiz(status);

CREATE INDEX IF NOT EXISTS ix_skill_module_question_quiz_order
    ON public.skill_module_quiz_question(skill_module_quiz_id, order_index);

CREATE INDEX IF NOT EXISTS ix_skill_module_option_question_order
    ON public.skill_module_quiz_option(skill_module_quiz_question_id, order_index);

CREATE INDEX IF NOT EXISTS ix_skill_module_enrollment_user_id
    ON public.skill_module_enrollment(user_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_enrollment_module_id
    ON public.skill_module_enrollment(skill_module_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_attempt_enrollment_id
    ON public.skill_module_quiz_attempt(skill_module_enrollment_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_attempt_user_id
    ON public.skill_module_quiz_attempt(user_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_answer_attempt_id
    ON public.skill_module_quiz_answer(skill_module_quiz_attempt_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_chunk_module_id
    ON public.skill_module_chunk(skill_module_id);

CREATE INDEX IF NOT EXISTS ix_skill_module_chunk_lesson_id
    ON public.skill_module_chunk(skill_module_lesson_id);

-- CREATE INDEX IF NOT EXISTS ix_skill_module_chunk_embedding
--     ON public.skill_module_chunk
--     USING ivfflat (embedding vector_cosine_ops)
--     WITH (lists = 100);

CREATE TABLE IF NOT EXISTS public.user_insight
(
    insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    metadata jsonb,

    CONSTRAINT fk_user_insight_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE
);

-- =========================
-- GitHub Repository
-- =========================

CREATE TABLE IF NOT EXISTS public.repository
(
    repository_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,

    github_repo_id bigint NOT NULL,
    name varchar(150) NOT NULL,
    full_name varchar(255) NOT NULL,
    html_url text NOT NULL,
    description text,

    primary_language varchar(50),
    stars int NOT NULL DEFAULT 0,
    forks int NOT NULL DEFAULT 0,

    is_private bool NOT NULL DEFAULT false,
    is_selected_for_portfolio bool NOT NULL DEFAULT true,

    github_created_at timestamptz,
    github_updated_at timestamptz,
    synced_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_repository_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_github_repo
        UNIQUE (user_id, github_repo_id)
);

CREATE TABLE IF NOT EXISTS public.repo_insight
(
    insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    repository_id uuid NOT NULL,

    summary text,
    tech_stack jsonb NOT NULL DEFAULT '[]'::jsonb,
    detected_skills jsonb NOT NULL DEFAULT '[]'::jsonb,
    project_type varchar(100),

    analysis_status varchar(50) NOT NULL DEFAULT 'completed',
    readme_hash varchar(64),
    readme_truncated bool NOT NULL DEFAULT false,
    ai_model varchar(100),
    error_message text,

    analyzed_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_repo_insight_repository_id
        FOREIGN KEY (repository_id)
        REFERENCES public.repository(repository_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_repo_insight_repository_id
        UNIQUE (repository_id),

    CONSTRAINT chk_repo_insight_analysis_status
        CHECK (analysis_status IN ('pending', 'completed', 'failed'))
);

-- =========================
-- Payment
-- =========================

CREATE TABLE IF NOT EXISTS public.invoice
(
    invoice_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    total_amount decimal(12, 2) NOT NULL,
    currency varchar(10) NOT NULL DEFAULT 'VND',
    status varchar(30) NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_invoice_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."user"(user_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.payment_transaction
(
    transaction_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id uuid NOT NULL,
    gateway varchar(30),
    gateway_transaction_id varchar(100),
    amount decimal(12, 2) NOT NULL,
    status varchar(30) NOT NULL,
    webhook_payload jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_payment_transaction_invoice_id
        FOREIGN KEY (invoice_id)
        REFERENCES public.invoice(invoice_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.skill_gap_analysis_history
(
    skill_gap_analysis_history_id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    career_role_id UUID NOT NULL,
    career_role_slug VARCHAR(200) NOT NULL,
    career_role_name VARCHAR(500) NOT NULL,
    readiness_percent NUMERIC(5,2) NOT NULL,
    skill_coverage_percent NUMERIC(5,2) NOT NULL,
    snapshot_json JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_skill_gap_history_user
        FOREIGN KEY(user_id)
        REFERENCES "user"(user_id),

    CONSTRAINT fk_skill_gap_history_role
        FOREIGN KEY(career_role_id)
        REFERENCES career_role(career_role_id)
);