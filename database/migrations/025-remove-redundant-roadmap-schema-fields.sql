BEGIN;

-- ============================================================
-- roadmap
-- ============================================================

DROP INDEX IF EXISTS public.ix_roadmap_source_type;
DROP INDEX IF EXISTS public.ix_roadmap_type_visibility;

ALTER TABLE IF EXISTS public.roadmap
    DROP COLUMN IF EXISTS roadmap_type,
    DROP COLUMN IF EXISTS source_type;

-- ============================================================
-- roadmap_version
-- ============================================================

DROP INDEX IF EXISTS public.ix_roadmap_version_generation_status;

ALTER TABLE IF EXISTS public.roadmap_version
    DROP COLUMN IF EXISTS generation_prompt,
    DROP COLUMN IF EXISTS generation_model,
    DROP COLUMN IF EXISTS generation_status,
    DROP COLUMN IF EXISTS generation_context,
    DROP COLUMN IF EXISTS generation_error;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'roadmap_version'
          AND column_name = 'generated_by_user_id'
    )
    AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'roadmap_version'
          AND column_name = 'created_by_user_id'
    ) THEN
        ALTER TABLE public.roadmap_version
            RENAME COLUMN generated_by_user_id TO created_by_user_id;
    END IF;
END $$;

DO $$
BEGIN
    IF to_regclass('public.roadmap_version') IS NOT NULL
       AND EXISTS (
           SELECT 1
           FROM pg_constraint
           WHERE conrelid = to_regclass('public.roadmap_version')
             AND conname = 'fk_roadmap_version_generated_by_user'
       )
       AND NOT EXISTS (
           SELECT 1
           FROM pg_constraint
           WHERE conrelid = to_regclass('public.roadmap_version')
             AND conname = 'fk_roadmap_version_created_by_user'
       ) THEN
        ALTER TABLE public.roadmap_version
            RENAME CONSTRAINT fk_roadmap_version_generated_by_user
            TO fk_roadmap_version_created_by_user;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'i'
          AND c.relname = 'ix_roadmap_version_generated_by_user_id'
    )
    AND NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'i'
          AND c.relname = 'ix_roadmap_version_created_by_user_id'
    ) THEN
        ALTER INDEX public.ix_roadmap_version_generated_by_user_id
            RENAME TO ix_roadmap_version_created_by_user_id;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_roadmap_version_created_by_user_id
    ON public.roadmap_version (created_by_user_id);

-- ============================================================
-- roadmap_node
-- ============================================================

DROP INDEX IF EXISTS public.ix_roadmap_node_layout_group;
DROP INDEX IF EXISTS public.ix_roadmap_node_layout_rank_order;
DROP INDEX IF EXISTS public.ix_roadmap_node_position;

ALTER TABLE IF EXISTS public.roadmap_node
    DROP COLUMN IF EXISTS reason,
    DROP COLUMN IF EXISTS priority,
    DROP COLUMN IF EXISTS position_x,
    DROP COLUMN IF EXISTS position_y,
    DROP COLUMN IF EXISTS layout_group,
    DROP COLUMN IF EXISTS layout_rank,
    DROP COLUMN IF EXISTS layout_order;

-- ============================================================
-- roadmap_node_resource
-- ============================================================

ALTER TABLE IF EXISTS public.roadmap_node_resource
    DROP COLUMN IF EXISTS order_index,
    DROP COLUMN IF EXISTS is_primary;

-- ============================================================
-- user_node_progress
-- ============================================================

ALTER TABLE IF EXISTS public.user_node_progress
    DROP COLUMN IF EXISTS learner_note,
    DROP COLUMN IF EXISTS evidence_url;

COMMIT;
