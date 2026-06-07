-- =========================
-- User, Role, Permission
-- =========================
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS public.permission
(
    permission_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_name varchar(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public.role
(
    role_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name varchar(15) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public.user
(
    user_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username varchar(50) UNIQUE NOT NULL,
    username_normalized varchar(50) UNIQUE NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,

    CONSTRAINT chk_user_status
        CHECK (status in ('active', 'suspended', 'deleted'))
);

CREATE TABLE IF NOT EXISTS public.user_profile
(
    user_id uuid PRIMARY KEY,

    display_name varchar(50),
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
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
        ON DELETE CASCADE
);

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
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
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
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),    permission_id uuid NOT NULL,
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
        REFERENCES public.user(user_id)
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

    CONSTRAINT fk_user_auth_provider_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_provider_identity
        UNIQUE (provider, provider_user_id),

    CONSTRAINT uq_user_provider
        UNIQUE (user_id, provider)
);

CREATE TABLE IF NOT EXISTS public.email_verification_token
(
    verification_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    user_id uuid NOT NULL,

    provider varchar(50) NOT NULL,
    email varchar(255) NOT NULL,

    purpose varchar(50) NOT NULL,
    otp_hash text NOT NULL,

    expires_at timestamptz NOT NULL,
    used_at timestamptz NULL,

    attempt_count int NOT NULL DEFAULT 0,
    max_attempts int NOT NULL DEFAULT 5,

    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_email_verification_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE
);

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
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
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
-- Uploaded AI/RAG Resources
-- =========================

CREATE TABLE IF NOT EXISTS public.resource
(
	resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	skill_id uuid NOT NULL,
	title varchar(100),
	url varchar(100) NOT NULL,
	created_at timestamptz default now(),
	metadata jsonb,

	CONSTRAINT fk_skill_id
		FOREIGN KEY (skill_id)
		REFERENCES public.skill(skill_id)
);

CREATE TABLE IF NOT EXISTS public.my_resource
(
	resource_id uuid PRIMARY KEY,
    
	CONSTRAINT fk_my_resource_id
		FOREIGN KEY (resource_id)
		REFERENCES public.resource(resource_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.other_resource
(
	resource_id uuid PRIMARY KEY,
	resource_type varchar(100),
	provider varchar(100),

	CONSTRAINT fk_other_resource_id
		FOREIGN KEY (resource_id)
		REFERENCES public.resource(resource_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.resource_chunk
(
    chunk_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_id uuid NOT NULL,
    chunk_content text NOT NULL,
	embedding vector(3072),

    CONSTRAINT fk_chunk_resource
        FOREIGN KEY (resource_id)
        REFERENCES public.resource(resource_id) ON DELETE CASCADE
);

-- =========================
-- Chatbot AI
-- =========================

CREATE TABLE IF NOT EXISTS public.conversation
(
    conversation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    resource_id uuid NOT NULL,
    title varchar(100),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_conversation_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_conversation_resource_id
        FOREIGN KEY (resource_id)
        REFERENCES public.resource(resource_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.chatbot_message
(
    request_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id uuid NOT NULL,
    content_message text NOT NULL,
    metadata jsonb,

    CONSTRAINT fk_chatbot_message_conversation_id
        FOREIGN KEY (conversation_id)
        REFERENCES public.conversation(conversation_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.user_insight
(
    insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    metadata jsonb,

    CONSTRAINT fk_user_insight_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
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
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_github_repo
        UNIQUE (user_id, github_repo_id)
);

CREATE TABLE IF NOT EXISTS public.repo_insight
(
    insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    repository_id uuid NOT NULL,
    summary text,
    tech_stack jsonb,
    detected_skills jsonb,
    project_type varchar(100),
    analyzed_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_repo_insight_repository_id
        FOREIGN KEY (repository_id)
        REFERENCES public.repository(repository_id)
        ON DELETE CASCADE
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
        REFERENCES public.user(user_id)
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
