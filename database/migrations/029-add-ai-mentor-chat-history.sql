-- ============================================================
-- 029 - Add AI Mentor chat history
-- Adds conversation/message storage for the AI Virtual Mentor.
-- ============================================================
BEGIN;

CREATE TABLE IF NOT EXISTS public.ai_mentor_conversation
(
	ai_mentor_conversation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,

	title varchar(255) NOT NULL DEFAULT 'New conversation',
	page_context varchar(100) NOT NULL DEFAULT 'roadmap_selection',

	archived_at timestamptz,

	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),

	CONSTRAINT fk_ai_mentor_conversation_user
		FOREIGN KEY (user_id)
		REFERENCES public."user"(user_id)
		ON DELETE CASCADE		
);

CREATE INDEX IF NOT EXISTS ix_ai_mentor_conversation_user_updated_at
    ON public.ai_mentor_conversation(user_id, updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_ai_mentor_conversation_user_active
    ON public.ai_mentor_conversation(user_id, archived_at)
    WHERE archived_at IS NULL;


CREATE TABLE IF NOT EXISTS public.ai_mentor_message
(
    ai_mentor_message_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    ai_mentor_conversation_id uuid NOT NULL,

    role varchar(30) NOT NULL,
    content text NOT NULL,

    sources jsonb NOT NULL DEFAULT '[]'::jsonb,
    ai_model varchar(100),

    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_ai_mentor_message_conversation
        FOREIGN KEY (ai_mentor_conversation_id)
        REFERENCES public.ai_mentor_conversation(ai_mentor_conversation_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_ai_mentor_message_role
        CHECK (role IN ('user', 'assistant'))
);

CREATE INDEX IF NOT EXISTS ix_ai_mentor_message_conversation_created_at
    ON public.ai_mentor_message(ai_mentor_conversation_id, created_at ASC);