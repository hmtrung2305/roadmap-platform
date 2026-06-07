-- ============================================================
-- 002-roadmap-tables.sql
-- Purpose:
--   Replace the old roadmap schema with the new roadmap catalog,
--   versioning, node, resource, enrollment, and progress structure.
--
-- Notes:
--   - This is a DB-first SQL migration.
--   - This migration drops old roadmap-specific tables.
--   - It preserves public.resource, public.resource_chunk,
--     public.conversation, and public.chatbot_message because those
--     are used by the existing AI/RAG resource upload feature.
--   - The existing public.skill table is modernized and reused instead
--     of dropped, because public.resource currently references it.
-- ============================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ============================================================
-- Drop old roadmap/progress mapping tables
-- ============================================================

DROP TABLE IF EXISTS public.user_skill_progress CASCADE;
DROP TABLE IF EXISTS public.user_roadmap_status CASCADE;
DROP TABLE IF EXISTS public.node_skill CASCADE;
DROP TABLE IF EXISTS public.roadmap_edge CASCADE;
DROP TABLE IF EXISTS public.roadmap_node CASCADE;
DROP TABLE IF EXISTS public.roadmap CASCADE;
DROP TABLE IF EXISTS public.specialty CASCADE;

-- ============================================================
-- Skill Taxonomy
-- Keep this separate from roadmap_node.
-- A skill is a reusable concept such as React, SQL, REST API, Git.
-- A roadmap node is a learning step such as "Learn React Hooks".
-- ============================================================

CREATE TABLE IF NOT EXISTS public.skill
(
    skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100),
    slug varchar(120),
    description text,
    category varchar(100),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
    -- Old schema used skill_name. New schema uses name.
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'skill_name'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'name'
    ) THEN
        ALTER TABLE public.skill RENAME COLUMN skill_name TO name;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'name'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN name varchar(100);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'slug'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN slug varchar(120);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'category'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN category varchar(100);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'is_active'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN is_active boolean NOT NULL DEFAULT true;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'created_at'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN created_at timestamptz NOT NULL DEFAULT now();
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'skill'
          AND column_name = 'updated_at'
    ) THEN
        ALTER TABLE public.skill ADD COLUMN updated_at timestamptz NOT NULL DEFAULT now();
    END IF;
END $$;

UPDATE public.skill
SET name = 'unnamed-skill-' || skill_id::text
WHERE name IS NULL OR btrim(name) = '';

UPDATE public.skill
SET slug = trim(both '-' from regexp_replace(lower(name), '[^a-z0-9]+', '-', 'g'))
WHERE slug IS NULL OR btrim(slug) = '';

-- Ensure generated slugs are never empty.
UPDATE public.skill
SET slug = 'skill-' || skill_id::text
WHERE slug IS NULL OR btrim(slug) = '';

ALTER TABLE public.skill
    ALTER COLUMN name SET NOT NULL,
    ALTER COLUMN slug SET NOT NULL;

ALTER TABLE public.skill
    DROP CONSTRAINT IF EXISTS uq_skill_name,
    DROP CONSTRAINT IF EXISTS uq_skill_slug;

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_name
    ON public.skill(name);

CREATE UNIQUE INDEX IF NOT EXISTS uq_skill_slug
    ON public.skill(slug);

-- ============================================================
-- Career Role
-- Examples: Frontend Developer, Backend Developer, Data Analyst.
-- ============================================================

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

-- ============================================================
-- Learning Resource Catalog
-- Static roadmap resources, separate from uploaded RAG resources.
-- ============================================================

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

-- Optional mapping: one resource can cover multiple skills.
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

-- ============================================================
-- Roadmap
-- A roadmap is the stable parent object.
-- Versions hold the actual published/draft node graph.
-- ============================================================

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

-- ============================================================
-- Roadmap Node
-- Nodes are visible roadmap units. Phase and choice_group nodes are computed; topic, choice_option, checkpoint, and project nodes are user-trackable.
-- ============================================================

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

-- ============================================================
-- Roadmap Edge / Layout + Progress Semantics
-- Edges distinguish sequence, containment, choice, dependency, unlock, and recommendation relationships.
-- ============================================================

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

-- ============================================================
-- Node Resource Mapping
-- Links roadmap nodes to static catalog resources.
-- ============================================================

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

-- ============================================================
-- Enrollment
-- User starts a specific published roadmap version.
-- ============================================================

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

-- ============================================================
-- User Node Progress
-- Stores manual user progress only. Locked/phase/choice_group statuses are computed by the backend.
-- ============================================================

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

-- ============================================================
-- Progress Audit Event
-- Append-only history for progress changes.
-- ============================================================

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

-- ============================================================
-- Indexes
-- ============================================================

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

COMMIT;
