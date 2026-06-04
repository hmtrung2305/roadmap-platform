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

CREATE TABLE IF NOT EXISTS public.specialty
(
    specialty_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    specialty_name varchar(100) NOT NULL UNIQUE,
    description text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.roadmap
(
    roadmap_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    specialty_id uuid NOT NULL,
    roadmap_name varchar(100) NOT NULL,
    version int NOT NULL DEFAULT 1,
    is_active bool NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_specialty_id
        FOREIGN KEY (specialty_id)
        REFERENCES public.specialty(specialty_id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS public.roadmap_node
(
    node_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_id uuid NOT NULL,
    title varchar(50) NOT NULL,
    position_x float,
    position_y float,
    description text,
    is_mandatory bool NOT NULL DEFAULT true,

    CONSTRAINT fk_roadmap_node_roadmap_id
        FOREIGN KEY (roadmap_id)
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.roadmap_edge
(
    edge_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_id uuid NOT NULL,
    ancestor_node_id uuid NOT NULL,
    descendant_node_id uuid NOT NULL,

    CONSTRAINT fk_roadmap_edge_roadmap_id
        FOREIGN KEY (roadmap_id)
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_ancestor_node_id
        FOREIGN KEY (ancestor_node_id)
        REFERENCES public.roadmap_node(node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_descendant_node_id
        FOREIGN KEY (descendant_node_id)
        REFERENCES public.roadmap_node(node_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_roadmap_edge
        UNIQUE (roadmap_id, ancestor_node_id, descendant_node_id),

    CONSTRAINT chk_roadmap_edge_not_self
        CHECK (ancestor_node_id <> descendant_node_id)
);

CREATE TABLE IF NOT EXISTS public.skill
(
    skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name varchar(100) NOT NULL UNIQUE,
    description text
);

CREATE TABLE IF NOT EXISTS public.node_skill
(
    node_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_node_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    CONSTRAINT fk_node_skill_roadmap_node_id
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public.roadmap_node(node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_node_skill_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_node_skill
        UNIQUE (roadmap_node_id, skill_id)
);

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

CREATE TABLE IF NOT EXISTS public.user_roadmap_status
(
    enrollment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    roadmap_id uuid NOT NULL,
    last_time timestamptz NOT NULL DEFAULT now(),
    status varchar(30),

    CONSTRAINT fk_user_roadmap_status_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_roadmap_status_roadmap_id
        FOREIGN KEY (roadmap_id)
        REFERENCES public.roadmap(roadmap_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_roadmap_status
        UNIQUE (user_id, roadmap_id)
);

CREATE TABLE IF NOT EXISTS public.user_skill_progress
(
    progress_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    skill_id uuid NOT NULL,
    status varchar(30),
    unlock_method jsonb,
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_skill_progress_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_skill_progress_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_skill_progress
        UNIQUE (user_id, skill_id)
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