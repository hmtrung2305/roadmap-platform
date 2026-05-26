DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- =========================
-- User, Role, Permission
-- =========================

CREATE TABLE IF NOT EXISTS public."Permission"
(
    permission_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_name varchar(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public."Role"
(
    role_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name varchar(15) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public."User"
(
    user_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username varchar(50) UNIQUE,
    display_name varchar(100),
    gender varchar(20),
    phone varchar(20),
    bio varchar(500),
    avatar_url text,
    website_url text,
    github_url text,
    linkedin_url text,
    status int NOT NULL DEFAULT 1,
    birthdate date,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public."Permission_Role"
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_id uuid NOT NULL,
    role_id uuid NOT NULL,

    CONSTRAINT fk_permission_role_permission_id
        FOREIGN KEY (permission_id)
        REFERENCES public."Permission"(permission_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_permission_role_role_id
        FOREIGN KEY (role_id)
        REFERENCES public."Role"(role_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_permission_role
        UNIQUE (permission_id, role_id)
);

CREATE TABLE IF NOT EXISTS public."User_Role"
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id uuid NOT NULL,
    user_id uuid NOT NULL,

    CONSTRAINT fk_user_role_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_role_role_id
        FOREIGN KEY (role_id)
        REFERENCES public."Role"(role_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_role
        UNIQUE (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS public."User_Auth_Provider"
(
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    email varchar(100),
    password_hash text,
    provider varchar(100) NOT NULL,
    provider_user_id varchar(255) NOT NULL,
    provider_username varchar(50),
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_auth_provider_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_provider_identity
        UNIQUE (provider, provider_user_id),

    CONSTRAINT uq_user_provider
        UNIQUE (user_id, provider)
);

-- =========================
-- Roadmap
-- =========================

CREATE TABLE IF NOT EXISTS public."Specialty"
(
    specialty_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    specialty_name varchar(100) NOT NULL UNIQUE,
    description text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public."Roadmap"
(
    roadmap_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    specialty_id uuid NOT NULL,
    roadmap_name varchar(100) NOT NULL,
    version int NOT NULL DEFAULT 1,
    is_active bool NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_roadmap_specialty_id
        FOREIGN KEY (specialty_id)
        REFERENCES public."Specialty"(specialty_id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS public."Roadmap_Node"
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
        REFERENCES public."Roadmap"(roadmap_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."Roadmap_Edge"
(
    edge_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_id uuid NOT NULL,
    ancestor_node_id uuid NOT NULL,
    descendant_node_id uuid NOT NULL,

    CONSTRAINT fk_roadmap_edge_roadmap_id
        FOREIGN KEY (roadmap_id)
        REFERENCES public."Roadmap"(roadmap_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_ancestor_node_id
        FOREIGN KEY (ancestor_node_id)
        REFERENCES public."Roadmap_Node"(node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_roadmap_edge_descendant_node_id
        FOREIGN KEY (descendant_node_id)
        REFERENCES public."Roadmap_Node"(node_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_roadmap_edge
        UNIQUE (roadmap_id, ancestor_node_id, descendant_node_id),

    CONSTRAINT chk_roadmap_edge_not_self
        CHECK (ancestor_node_id <> descendant_node_id)
);

CREATE TABLE IF NOT EXISTS public."Skill"
(
    skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name varchar(100) NOT NULL UNIQUE,
    description text
);

CREATE TABLE IF NOT EXISTS public."Node_Skill"
(
    node_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_node_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    CONSTRAINT fk_node_skill_roadmap_node_id
        FOREIGN KEY (roadmap_node_id)
        REFERENCES public."Roadmap_Node"(node_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_node_skill_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public."Skill"(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_node_skill
        UNIQUE (roadmap_node_id, skill_id)
);

CREATE TABLE IF NOT EXISTS public."My_Resource"
(
    resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_id uuid NOT NULL,
    title varchar(100) NOT NULL,
    url text,
    created_at timestamptz NOT NULL DEFAULT now(),
    metadata jsonb,

    CONSTRAINT fk_my_resource_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public."Skill"(skill_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."Learning_Resource"
(
    resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_id uuid NOT NULL,
    title varchar(100) NOT NULL,
    url text,
    resource_type varchar(100),
    provider varchar(100),
    created_at timestamptz NOT NULL DEFAULT now(),
    metadata jsonb,

    CONSTRAINT fk_learning_resource_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public."Skill"(skill_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."User_Roadmap_Status"
(
    enrollment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    roadmap_id uuid NOT NULL,
    last_time timestamptz NOT NULL DEFAULT now(),
    status varchar(100),

    CONSTRAINT fk_user_roadmap_status_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_roadmap_status_roadmap_id
        FOREIGN KEY (roadmap_id)
        REFERENCES public."Roadmap"(roadmap_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_roadmap_status
        UNIQUE (user_id, roadmap_id)
);

CREATE TABLE IF NOT EXISTS public."User_Skill_Progress"
(
    progress_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    skill_id uuid NOT NULL,
    status varchar(100),
    unlock_method jsonb,
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_skill_progress_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_skill_progress_skill_id
        FOREIGN KEY (skill_id)
        REFERENCES public."Skill"(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_skill_progress
        UNIQUE (user_id, skill_id)
);

-- =========================
-- Chatbot AI
-- =========================

CREATE TABLE IF NOT EXISTS public."Conversation"
(
    conversation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    resource_id uuid NOT NULL,
    title varchar(100),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_conversation_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_conversation_resource_id
        FOREIGN KEY (resource_id)
        REFERENCES public."Learning_Resource"(resource_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."Chatbot_Message"
(
    request_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id uuid NOT NULL,
    content_message text NOT NULL,
    metadata jsonb,

    CONSTRAINT fk_chatbot_message_conversation_id
        FOREIGN KEY (conversation_id)
        REFERENCES public."Conversation"(conversation_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."User_Insight"
(
    insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    metadata jsonb,

    CONSTRAINT fk_user_insight_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE
);

-- =========================
-- GitHub Repository
-- =========================

CREATE TABLE IF NOT EXISTS public."Repository"
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
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_user_github_repo
        UNIQUE (user_id, github_repo_id)
);

CREATE TABLE IF NOT EXISTS public."Repo_Insight"
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
        REFERENCES public."Repository"(repository_id)
        ON DELETE CASCADE
);

-- =========================
-- Job
-- =========================

CREATE TABLE IF NOT EXISTS public."Company"
(
    company_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name varchar(100) NOT NULL,
    company_location varchar(100),
    description text
);

CREATE TABLE IF NOT EXISTS public."Job"
(
    job_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL,
    title varchar(100) NOT NULL,
    job_location varchar(100),
    job_extension jsonb,
    job_detected_extension jsonb,
    description text,
    apply_option jsonb,

    CONSTRAINT fk_job_company_id
        FOREIGN KEY (company_id)
        REFERENCES public."Company"(company_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."Skill_Trend"
(
    trend_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name varchar(100) NOT NULL,
    frequency int NOT NULL DEFAULT 0,
    analyzed_date timestamptz NOT NULL DEFAULT now()
);

-- =========================
-- Payment
-- =========================

CREATE TABLE IF NOT EXISTS public."Invoice"
(
    invoice_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    total_amount decimal(12, 2) NOT NULL,
    currency varchar(10) NOT NULL DEFAULT 'VND',
    status varchar(10) NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_invoice_user_id
        FOREIGN KEY (user_id)
        REFERENCES public."User"(user_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."Payment_Transaction"
(
    transaction_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id uuid NOT NULL,
    gateway varchar(30),
    gateway_transaction_id varchar(100),
    amount decimal(12, 2) NOT NULL,
    status varchar(10) NOT NULL,
    webhook_payload jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_payment_transaction_invoice_id
        FOREIGN KEY (invoice_id)
        REFERENCES public."Invoice"(invoice_id)
        ON DELETE CASCADE
);
