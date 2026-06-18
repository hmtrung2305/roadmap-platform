ALTER TABLE public.skill_module_lesson
ADD COLUMN IF NOT EXISTS indexing_status varchar(30) NOT NULL DEFAULT 'pending',
ADD COLUMN IF NOT EXISTS indexed_at timestamptz,
ADD COLUMN IF NOT EXISTS indexing_error text;

UPDATE public.skill_module_lesson lesson
SET indexing_status = 'indexed',
    indexed_at = COALESCE(lesson.updated_at, now()),
    indexing_error = NULL
WHERE EXISTS (
    SELECT 1
    FROM public.skill_module_chunk chunk
    WHERE chunk.skill_module_lesson_id = lesson.skill_module_lesson_id
);

UPDATE public.skill_module_lesson lesson
SET indexing_status = 'failed',
    indexing_error = 'No chunks found for this lesson. Re-upload or re-index the lesson.'
WHERE NOT EXISTS (
    SELECT 1
    FROM public.skill_module_chunk chunk
    WHERE chunk.skill_module_lesson_id = lesson.skill_module_lesson_id
);

ALTER TABLE public.skill_module_lesson
ADD CONSTRAINT chk_skill_module_lesson_indexing_status
CHECK (indexing_status IN ('pending', 'indexing', 'indexed', 'failed', 'needs_reindex'));

CREATE INDEX IF NOT EXISTS ix_skill_module_lesson_indexing_status
ON public.skill_module_lesson (skill_module_id, indexing_status);