-- Migration 011: create skill module schema and remove legacy uploaded resource tables

BEGIN;

-- Old uploaded resource/RAG tables are replaced by skill_module_lesson and skill_module_chunk.
-- The old resource-scoped chatbot tables depend on public.resource, so they are removed with the old resource flow.
DROP TABLE IF EXISTS public.chatbot_message CASCADE;
DROP TABLE IF EXISTS public.conversation CASCADE;
DROP TABLE IF EXISTS public.resource_chunk CASCADE;
DROP TABLE IF EXISTS public.other_resource CASCADE;
DROP TABLE IF EXISTS public.my_resource CASCADE;
DROP TABLE IF EXISTS public.resource CASCADE;

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
        CHECK (content_size_bytes IS NULL OR content_size_bytes >= 0)
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

COMMIT;
